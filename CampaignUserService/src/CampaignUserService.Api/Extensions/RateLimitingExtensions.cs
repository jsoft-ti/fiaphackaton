using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace CampaignUserService.Api.Extensions;

public static class RateLimitingExtensions
{
    public const string GlobalPolicy = "global";
    public const string AuthPolicy = "auth";

    public static IServiceCollection AddApiRateLimiting(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            options.OnRejected = async (context, cancellationToken) =>
            {
                context.HttpContext.Response.ContentType = "application/problem+json";
                await context.HttpContext.Response.WriteAsJsonAsync(
                    new
                    {
                        type = "https://httpstatuses.io/429",
                        title = "Muitas requisições. Tente novamente mais tarde.",
                        status = StatusCodes.Status429TooManyRequests,
                        code = "rate_limit_exceeded"
                    },
                    cancellationToken);
            };

            // General API traffic: 100 requests/minute per client IP.
            options.AddPolicy(GlobalPolicy, httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: GetPartitionKey(httpContext),
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 100,
                        Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 0
                    }));

            // Authentication endpoints (login/register/forgot-password) are more sensitive
            // to brute-force/credential-stuffing: 10 requests/minute per client IP.
            options.AddPolicy(AuthPolicy, httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: GetPartitionKey(httpContext),
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 10,
                        Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 0
                    }));
        });

        return services;
    }

    private static string GetPartitionKey(HttpContext httpContext) =>
        httpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault()?.Split(',')[0].Trim()
        ?? httpContext.Connection.RemoteIpAddress?.ToString()
        ?? "unknown";
}
