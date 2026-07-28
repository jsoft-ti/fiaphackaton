using System.Text;
using DonationService.Api.Options;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace DonationService.Api.Extensions;

public static class JwtAuthenticationExtensions
{
    public const string DoadorOnlyPolicy = "DoadorOnly";

    public const string GestorOngOnlyPolicy = "GestorOngOnly";

    public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        var jwtSection = configuration.GetSection(JwtSettings.SectionName);
        var jwtSettings = jwtSection.Get<JwtSettings>()
            ?? throw new InvalidOperationException("Jwt configuration section is missing.");

        if (string.IsNullOrWhiteSpace(jwtSettings.SecretKey))
        {
            throw new InvalidOperationException("Jwt:SecretKey must be configured (shared with CampaignUserService).");
        }

        services.Configure<JwtSettings>(jwtSection);

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwtSettings.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwtSettings.Audience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.SecretKey)),
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromSeconds(jwtSettings.ClockSkewSeconds),
                };

                options.Events = new JwtBearerEvents
                {
                    OnAuthenticationFailed = context =>
                    {
                        var logger = context.HttpContext.RequestServices
                            .GetRequiredService<ILoggerFactory>()
                            .CreateLogger("JwtAuthentication");

                        logger.LogWarning(context.Exception, "JWT authentication failed.");

                        return Task.CompletedTask;
                    },
                };
            });

        services.AddAuthorizationBuilder()
            .AddPolicy(DoadorOnlyPolicy, policy => policy.RequireRole("Doador"))
            .AddPolicy(GestorOngOnlyPolicy, policy => policy.RequireRole("GestorOng"));

        return services;
    }
}
