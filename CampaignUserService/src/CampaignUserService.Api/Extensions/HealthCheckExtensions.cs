using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;

namespace CampaignUserService.Api.Extensions;

public static class HealthCheckExtensions
{
    public static IServiceCollection AddApplicationHealthChecks(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");

        services.AddHealthChecks()
            .AddNpgSql(
                connectionString,
                name: "postgresql",
                tags: ["ready", "db"]);

        return services;
    }

    public static WebApplication MapApplicationHealthChecks(this WebApplication app)
    {
        // Liveness: process is up, no external dependency checks.
        app.MapHealthChecks("/health/live", new HealthCheckOptions
        {
            Predicate = _ => false,
            ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
        });

        // Readiness: dependencies (database) must also be healthy.
        app.MapHealthChecks("/health/ready", new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("ready"),
            ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
        });

        // Aggregate health endpoint.
        app.MapHealthChecks("/health", new HealthCheckOptions
        {
            ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
        });

        return app;
    }
}
