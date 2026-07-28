using System.Reflection;
using CampaignUserService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CampaignUserService.Infrastructure.Persistence;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();

    public DbSet<Role> Roles => Set<Role>();

    public DbSet<UserRole> UserRoles => Set<UserRole>();

    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    public DbSet<PasswordResetToken> PasswordResetTokens => Set<PasswordResetToken>();

    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("campaign_user");
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

        // Global query filter: soft-deleted users are invisible unless explicitly ignored.
        modelBuilder.Entity<User>().HasQueryFilter(u => !u.IsDeleted);

        base.OnModelCreating(modelBuilder);
    }
}
