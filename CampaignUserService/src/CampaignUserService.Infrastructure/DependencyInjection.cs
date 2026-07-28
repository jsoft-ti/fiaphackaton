using CampaignUserService.Application.Common.Interfaces;
using CampaignUserService.Application.Common.Models;
using CampaignUserService.Domain.Repositories;
using CampaignUserService.Infrastructure.Options;
using CampaignUserService.Infrastructure.Persistence;
using CampaignUserService.Infrastructure.Persistence.Seed;
using CampaignUserService.Infrastructure.Security;
using CampaignUserService.Infrastructure.Services;
using CampaignUserService.SharedKernel.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CampaignUserService.Infrastructure;

/// <summary>
/// Composition root entry point for the Infrastructure layer: wires up
/// EF Core / PostgreSQL, repositories, security services and Options.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<JwtSettings>()
            .Bind(configuration.GetSection(JwtSettings.SectionName))
            .Validate(s => !string.IsNullOrWhiteSpace(s.Secret) && s.Secret.Length >= 32,
                "Jwt:Secret must be configured and be at least 32 characters long.")
            .ValidateOnStart();

        services.AddOptions<AdminSeedSettings>()
            .Bind(configuration.GetSection(AdminSeedSettings.SectionName));

        services.AddOptions<SmtpSettings>()
            .Bind(configuration.GetSection(SmtpSettings.SectionName));

        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");

        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(connectionString, npgsql =>
            {
                npgsql.EnableRetryOnFailure(3);
                npgsql.MigrationsHistoryTable("__ef_migrations_history", "campaign_user");
            }));

        services.AddHttpContextAccessor();

        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IAuditService, AuditService>();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<IEmailSender, SmtpEmailSender>();

        services.AddSingleton<IPasswordHasher, BCryptPasswordHasher>();
        services.AddSingleton<IJwtTokenService, JwtTokenService>();
        services.AddSingleton<IDateTimeProvider, DateTimeProvider>();

        services.AddScoped<DbSeeder>();

        return services;
    }
}
