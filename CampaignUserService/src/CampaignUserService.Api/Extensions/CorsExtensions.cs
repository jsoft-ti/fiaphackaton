namespace CampaignUserService.Api.Extensions;

public static class CorsExtensions
{
    public const string PolicyName = "CampaignUserServiceCors";

    public static IServiceCollection AddCorsPolicy(this IServiceCollection services, IConfiguration configuration)
    {
        var allowedOrigins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];

        services.AddCors(options =>
        {
            options.AddPolicy(PolicyName, policy =>
            {
                if (allowedOrigins.Length > 0)
                {
                    policy.WithOrigins(allowedOrigins)
                        .AllowAnyHeader()
                        .AllowAnyMethod()
                        .AllowCredentials();
                }
                else
                {
                    // No origins configured: fail closed (deny cross-origin browser calls)
                    // rather than silently allowing everything.
                    policy.WithOrigins(Array.Empty<string>());
                }
            });
        });

        return services;
    }
}
