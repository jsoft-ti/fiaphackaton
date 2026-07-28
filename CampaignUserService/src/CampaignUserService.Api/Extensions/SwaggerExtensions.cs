using Microsoft.OpenApi.Models;

namespace CampaignUserService.Api.Extensions;

public static class SwaggerExtensions
{
    public static IServiceCollection AddSwaggerDocumentation(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();

        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "CampaignUserService API",
                Version = "v1",
                Description = "Autenticação, autorização e gerenciamento de usuários do sistema de campanhas sociais.",
                Contact = new OpenApiContact { Name = "Campanhas Sociais - Plataforma" }
            });

            var jwtSecurityScheme = new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Description = "Informe o token JWT no formato: Bearer {seu token}",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.Http,
                Scheme = "Bearer",
                BearerFormat = "JWT",
                Reference = new OpenApiReference
                {
                    Id = "Bearer",
                    Type = ReferenceType.SecurityScheme
                }
            };

            options.AddSecurityDefinition(jwtSecurityScheme.Reference.Id, jwtSecurityScheme);
            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                { jwtSecurityScheme, Array.Empty<string>() }
            });
        });

        return services;
    }
}
