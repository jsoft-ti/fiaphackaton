using DonationService.Application;
using DonationService.Infrastructure;
using DonationService.Worker.Consumers;
using DonationService.Worker.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
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
    Log.Information("Starting DonationService.Worker");

    // WebApplication (not a plain generic host) so this Worker SDK project
    // can still expose /health, /health/live, /health/ready for the Docker
    // healthcheck, alongside the MassTransit consumer bus which runs as a
    // hosted service in the same process.
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((context, services, configuration) =>
        configuration
            .ReadFrom.Configuration(context.Configuration)
            .ReadFrom.Services(services)
            .Enrich.FromLogContext()
            .Enrich.WithExceptionDetails()
            .Enrich.WithEnvironmentName());

    builder.Services.AddApplication();
    builder.Services.AddMongoPersistence(builder.Configuration);
    builder.Services.AddWorkerCurrentUserAndTimeServices();
    builder.Services.AddDonationServiceConsumerMessaging<DonationCreatedConsumer>(builder.Configuration);
    builder.Services.AddWorkerHealthChecks(builder.Configuration);

    var app = builder.Build();

    app.UseSerilogRequestLogging();

    app.MapWorkerHealthChecks();

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "DonationService.Worker terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

/// <summary>Entry point partial class, exposed for MassTransit test-harness-based consumer tests.</summary>
public partial class Program
{
}
