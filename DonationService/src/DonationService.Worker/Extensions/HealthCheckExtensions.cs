using DonationService.Infrastructure.Messaging;
using DonationService.Infrastructure.Persistence.Mongo;
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DonationService.Worker.Extensions;

public static class HealthCheckExtensions
{
    public static IServiceCollection AddWorkerHealthChecks(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHealthChecks();

        return services;
    }

    public static WebApplication MapWorkerHealthChecks(this WebApplication app)
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
