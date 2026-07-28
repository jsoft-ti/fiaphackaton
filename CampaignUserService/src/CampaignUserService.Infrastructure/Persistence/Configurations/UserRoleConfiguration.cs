using CampaignUserService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CampaignUserService.Infrastructure.Persistence.Configurations;

public sealed class UserRoleConfiguration : IEntityTypeConfiguration<UserRole>
{
    public void Configure(EntityTypeBuilder<UserRole> builder)
    {
        builder.ToTable("user_roles");

        builder.HasKey(ur => ur.Id);
        builder.Property(ur => ur.Id).HasColumnName("id").ValueGeneratedNever();

        builder.Property(ur => ur.UserId).HasColumnName("user_id").IsRequired();
        builder.Property(ur => ur.RoleId).HasColumnName("role_id").IsRequired();

        builder.HasIndex(ur => new { ur.UserId, ur.RoleId }).IsUnique().HasDatabaseName("ix_user_roles_user_role");

        builder.HasOne(ur => ur.Role)
            .WithMany(r => r.UserRoles)
            .HasForeignKey(ur => ur.RoleId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(ur => ur.CreatedAtUtc).HasColumnName("created_at_utc").IsRequired();
        builder.Property(ur => ur.UpdatedAtUtc).HasColumnName("updated_at_utc");
        builder.Property(ur => ur.DeletedAtUtc).HasColumnName("deleted_at_utc");
        builder.Property(ur => ur.IsDeleted).HasColumnName("is_deleted").HasDefaultValue(false);
    }
}
