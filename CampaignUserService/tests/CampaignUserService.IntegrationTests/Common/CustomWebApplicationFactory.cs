using CampaignUserService.Infrastructure.Persistence;
using CampaignUserService.Infrastructure.Persistence.Seed;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;
using Xunit;

namespace CampaignUserService.IntegrationTests.Common;

/// <summary>
/// Spins up a real, ephemeral PostgreSQL instance (Testcontainers) for the
/// duration of the test run and boots the full Api host against it, so
/// integration tests exercise the real EF Core provider/SQL behavior
/// instead of an in-memory substitute.
/// </summary>
public sealed class CustomWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgresContainer = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithDatabase("campaign_user_service_tests")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    public const string TestAdminEmail = "admin.tests@campanhas-sociais.org";
    public const string TestAdminPassword = "AdminTests@123456";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((_, configBuilder) =>
        {
            configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = _postgresContainer.GetConnectionString(),
                ["Jwt:Secret"] = "integration-test-secret-key-with-at-least-32-characters",
                ["Jwt:Issuer"] = "CampaignUserService.IntegrationTests",
                ["Jwt:Audience"] = "CampaignUserService.IntegrationTests.Clients",
                ["Jwt:AccessTokenExpirationMinutes"] = "15",
                ["Jwt:RefreshTokenExpirationDays"] = "7",
                ["AdminSeed:FirstName"] = "Admin",
                ["AdminSeed:LastName"] = "Tests",
                ["AdminSeed:Email"] = TestAdminEmail,
                ["AdminSeed:Password"] = TestAdminPassword,
                ["Smtp:Enabled"] = "false",
                ["Database:AutoMigrateAndSeed"] = "false" // handled explicitly in InitializeAsync below.
            });
        });

        builder.ConfigureServices(services =>
        {
            services.AddScoped<DbSeeder>();
        });
    }

    public async Task InitializeAsync()
    {
        await _postgresContainer.StartAsync();

        using var scope = Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var seeder = scope.ServiceProvider.GetRequiredService<DbSeeder>();

        await dbContext.Database.EnsureCreatedAsync();
        await seeder.SeedAsync(applyMigrations: false);
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await _postgresContainer.StopAsync();
        await _postgresContainer.DisposeAsync();
    }
}
