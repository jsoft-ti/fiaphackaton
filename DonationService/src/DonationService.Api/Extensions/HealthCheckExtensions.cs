using DonationService.Infrastructure.Messaging;
using DonationService.Infrastructure.Persistence.Mongo;
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace DonationService.Api.Extensions;

public static class HealthCheckExtensions
{
    public static IServiceCollection AddDonationServiceHealthChecks(this IServiceCollection services, IConfiguration configuration)
    {
        var npgsqlConnectionString = configuration.GetConnectionString("DonationServiceDb") ?? string.Empty;

        services.AddHealthChecks()
            .AddNpgSql(npgsqlConnectionString, name: "postgresql", tags: new[] { "ready", "db" });

        return services;
    }

    public static WebApplication MapDonationServiceHealthChecks(this WebApplication app)
    {
        app.MapHealthChecks("/health", new HealthCheckOptions
        {
            ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse,
        });

        app.MapHealthChecks("/health/live", new HealthCheckOptions
        {
            Predicate = _ => false,
            ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse,
        });

        app.MapHealthChecks("/health/ready", new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("ready"),
            ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse,
        });

        return app;
    }
}
