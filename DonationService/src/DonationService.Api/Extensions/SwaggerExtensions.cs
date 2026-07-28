using Microsoft.OpenApi.Models;

namespace DonationService.Api.Extensions;

public static class SwaggerExtensions
{
    public static IServiceCollection AddSwaggerDocumentation(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();

        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "DonationService API",
                Version = "v1",
                Description = "Processes donations made by authenticated Doador users, publishing DonationCreatedEvent " +
                              "to RabbitMQ for asynchronous, eventually-consistent persistence in MongoDB.",
            });

            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Description = "JWT Bearer token issued by CampaignUserService. Example: \"Bearer {token}\"",
                Name = "Authorization",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.Http,
                Scheme = "Bearer",
                BearerFormat = "JWT",
            });

            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" },
                    },
                    Array.Empty<string>()
                },
            });
        });

        return services;
    }

    public static IApplicationBuilder UseSwaggerDocumentation(this WebApplication app)
    {
        app.UseSwagger();
        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint("/swagger/v1/swagger.json", "DonationService API v1");
            options.RoutePrefix = "swagger";
        });

        return app;
    }
}
