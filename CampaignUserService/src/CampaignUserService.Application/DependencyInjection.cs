using System.Reflection;
using CampaignUserService.Application.Common.Behaviors;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace CampaignUserService.Application;

/// <summary>
/// Composition root entry point for the Application layer. Keeps the Api
/// project free of any knowledge about MediatR/FluentValidation/AutoMapper
/// internals (Dependency Inversion).
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();

        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(assembly);
            cfg.AddOpenBehavior(typeof(LoggingBehavior<,>));
            cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
        });

        services.AddValidatorsFromAssembly(assembly);
        services.AddAutoMapper(assembly);

        return services;
    }
}
