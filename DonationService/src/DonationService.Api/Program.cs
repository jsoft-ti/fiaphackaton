using Asp.Versioning;
using DonationService.Api.Common;
using DonationService.Api.Endpoints;
using DonationService.Api.Extensions;
using DonationService.Api.Middleware;
using DonationService.Application;
using DonationService.Infrastructure;
using Serilog;
using Serilog.Exceptions;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .Enrich.FromLogContext()
    .Enrich.WithExceptionDetails()
    .Enrich.WithEnvironmentName()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    Log.Information("Starting DonationService.Api");

    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((context, services, configuration) =>
        configuration
            .ReadFrom.Configuration(context.Configuration)
            .ReadFrom.Services(services)
            .Enrich.FromLogContext()
            .Enrich.WithExceptionDetails()
            .Enrich.WithEnvironmentName());

    // Application / Infrastructure
    builder.Services.AddApplication();
    builder.Services.AddPostgresPersistence(builder.Configuration);
    builder.Services.AddMongoPersistence(builder.Configuration);
    builder.Services.AddCampaignServiceClient(builder.Configuration);
    builder.Services.AddCurrentUserAndTimeServices();
    builder.Services.AddDonationServiceProducerMessaging(builder.Configuration);

    // Api concerns
    builder.Services.AddJwtAuthentication(builder.Configuration);
    builder.Services.AddAuthorization();
    builder.Services.AddSwaggerDocumentation();
    builder.Services.AddDonationServiceHealthChecks(builder.Configuration);
    builder.Services.AddHttpContextAccessor();

    builder.Services.AddApiVersioning(options =>
    {
        options.DefaultApiVersion = new ApiVersion(1);
        options.AssumeDefaultVersionWhenUnspecified = true;
        options.ReportApiVersions = true;
        options.ApiVersionReader = new UrlSegmentApiVersionReader();
    }).AddApiExplorer(options =>
    {
        options.GroupNameFormat = "'v'VVV";
        options.SubstituteApiVersionInUrl = true;
    });

    builder.Services.AddCors(options =>
    {
        options.AddDefaultPolicy(policy =>
            policy.AllowAnyHeader().AllowAnyMethod().AllowAnyOrigin());
    });

    var app = builder.Build();

    app.UseSerilogRequestLogging();
    app.UseCorrelationId();
    app.UseGlobalExceptionMiddleware();

    if (app.Environment.IsDevelopment())
    {
        app.UseSwaggerDocumentation();
    }

    app.UseCors();
    app.UseAuthentication();
    app.UseAuthorization();

    app.MapDonationServiceHealthChecks();

    var apiVersionSet = app.NewApiVersionSet()
        .HasApiVersion(new ApiVersion(1))
        .ReportApiVersions()
        .Build();

    var versionedGroup = app.MapGroup("/api/v{version:apiVersion}")
        .WithApiVersionSet(apiVersionSet);

    versionedGroup.MapDonationEndpoints();

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "DonationService.Api terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

/// <summary>Entry point partial class, exposed for WebApplicationFactory-based integration tests.</summary>
public partial class Program
{
}
