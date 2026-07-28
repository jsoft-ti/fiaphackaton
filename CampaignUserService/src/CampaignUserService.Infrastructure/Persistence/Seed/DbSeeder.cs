using CampaignUserService.Application.Common.Interfaces;
using CampaignUserService.Application.Common.Models;
using CampaignUserService.Domain.Entities;
using CampaignUserService.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CampaignUserService.Infrastructure.Persistence.Seed;

/// <summary>
/// Idempotent startup seeder: guarantees the two system roles exist and, if
/// configured, creates the initial GestorOng administrator. Safe to run on
/// every application startup.
/// </summary>
public sealed class DbSeeder(
    ApplicationDbContext dbContext,
    IPasswordHasher passwordHasher,
    IOptions<AdminSeedSettings> adminSeedOptions,
    ILogger<DbSeeder> logger)
{
    /// <param name="applyMigrations">
    /// When true (default, production), pending EF Core migrations are applied
    /// before seeding. Integration tests that provision the schema themselves
    /// (e.g. via <c>EnsureCreatedAsync</c> against a Testcontainers database)
    /// should pass false to skip this step.
    /// </param>
    public async Task SeedAsync(bool applyMigrations = true, CancellationToken cancellationToken = default)
    {
        if (applyMigrations)
        {
            await dbContext.Database.MigrateAsync(cancellationToken);
        }

        var doadorRole = await EnsureRoleAsync(RoleName.Doador, "Doador: pode se cadastrar, autenticar e gerenciar o próprio perfil.", cancellationToken);
        var gestorRole = await EnsureRoleAsync(RoleName.GestorOng, "GestorOng: administra usuários, roles e campanhas da organização.", cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);

        await EnsureAdminUserAsync(gestorRole, cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<Role> EnsureRoleAsync(RoleName name, string description, CancellationToken cancellationToken)
    {
        var existing = await dbContext.Roles.FirstOrDefaultAsync(r => r.Name == name, cancellationToken);
        if (existing is not null)
        {
            return existing;
        }

        var role = Role.Create(name, description);
        dbContext.Roles.Add(role);
        logger.LogInformation("Seeded role {RoleName}", name);
        return role;
    }

    private async Task EnsureAdminUserAsync(Role gestorRole, CancellationToken cancellationToken)
    {
        var settings = adminSeedOptions.Value;

        if (string.IsNullOrWhiteSpace(settings.Email) || string.IsNullOrWhiteSpace(settings.Password))
        {
            logger.LogWarning(
                "AdminSeed:Email/Password not configured - skipping initial GestorOng administrator creation.");
            return;
        }

        var normalizedEmail = settings.Email.Trim().ToLowerInvariant();
        var exists = await dbContext.Users.IgnoreQueryFilters().AnyAsync(u => u.Email == normalizedEmail, cancellationToken);

        if (exists)
        {
            return;
        }

        var admin = User.Create(
            settings.FirstName,
            settings.LastName,
            settings.Email,
            passwordHasher.Hash(settings.Password),
            phoneNumber: null,
            cpf: null,
            birthDate: null);

        admin.AssignRole(gestorRole, DateTime.UtcNow);
        admin.ConfirmEmail(DateTime.UtcNow);

        dbContext.Users.Add(admin);
        logger.LogInformation("Seeded initial GestorOng administrator account {Email}", settings.Email);
    }
}
