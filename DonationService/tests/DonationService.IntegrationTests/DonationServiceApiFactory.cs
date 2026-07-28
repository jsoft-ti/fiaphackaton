using DonationService.Application.Common.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Testcontainers.MongoDb;
using Testcontainers.PostgreSql;
using Testcontainers.RabbitMq;
using Xunit;

namespace DonationService.IntegrationTests;

/// <summary>
/// Spins up real Postgres, MongoDB and RabbitMQ containers via Testcontainers
/// and boots DonationService.Api's actual <c>Program</c> against them -
/// requires a working Docker daemon to run (skip locally otherwise; this
/// runs in CI). <see cref="ICampaignServiceClient"/> is swapped for a stub so
/// these tests don't also require a live CampaignUserService instance.
/// </summary>
public sealed class DonationServiceApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    public const string JwtSecretKey = "integration-tests-super-secret-key-min-32-chars!!";
    public const string JwtIssuer = "CampaignUserService";
    public const string JwtAudience = "CampaignPlatform";

    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithDatabase("donationservice_tests")
        .WithUsername("donationservice")
        .WithPassword("donationservice")
        .Build();

    private readonly MongoDbContainer _mongo = new MongoDbBuilder()
        .WithImage("mongo:7")
        .Build();

    private readonly RabbitMqContainer _rabbitMq = new RabbitMqBuilder()
        .WithImage("rabbitmq:3.13-management-alpine")
        .Build();

    public StubCampaignServiceClient CampaignServiceClientStub { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((_, configBuilder) =>
        {
            configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DonationServiceDb"] = _postgres.GetConnectionString(),
                ["MongoDb:ConnectionString"] = _mongo.GetConnectionString(),
                ["MongoDb:DatabaseName"] = "donation_service_tests",
                ["RabbitMq:Host"] = _rabbitMq.Hostname,
                ["RabbitMq:Port"] = _rabbitMq.GetMappedPublicPort(5672).ToString(),
                ["RabbitMq:Username"] = "guest",
                ["RabbitMq:Password"] = "guest",
                ["Jwt:Issuer"] = JwtIssuer,
                ["Jwt:Audience"] = JwtAudience,
                ["Jwt:SecretKey"] = JwtSecretKey,
                ["CampaignService:BaseUrl"] = "http://localhost:1",
            });
        });

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<ICampaignServiceClient>();
            services.AddScoped<ICampaignServiceClient>(_ => CampaignServiceClientStub);
        });
    }

    public async Task InitializeAsync()
    {
        await Task.WhenAll(
            _postgres.StartAsync(),
            _mongo.StartAsync(),
            _rabbitMq.StartAsync());
    }

    public new async Task DisposeAsync()
    {
        await Task.WhenAll(
            _postgres.DisposeAsync().AsTask(),
            _mongo.DisposeAsync().AsTask(),
            _rabbitMq.DisposeAsync().AsTask());

        await base.DisposeAsync();
    }
}

public sealed class StubCampaignServiceClient : ICampaignServiceClient
{
    public CampaignValidationResult Result { get; set; } = new(true, true, true, "Test Campaign");

    public Task<CampaignValidationResult> ValidateCampaignAsync(Guid campaignId, CancellationToken cancellationToken) =>
        Task.FromResult(Result);
}
