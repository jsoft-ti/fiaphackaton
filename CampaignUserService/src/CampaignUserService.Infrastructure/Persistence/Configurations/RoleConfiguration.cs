using CampaignUserService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CampaignUserService.Infrastructure.Persistence.Configurations;

public sealed class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.ToTable("roles");

        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).HasColumnName("id").ValueGeneratedNever();

        builder.Property(r => r.Name).HasColumnName("name").HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.HasIndex(r => r.Name).IsUnique().HasDatabaseName("ix_roles_name");

        builder.Property(r => r.Description).HasColumnName("description").HasMaxLength(500).IsRequired();

        builder.Property(r => r.CreatedAtUtc).HasColumnName("created_at_utc").IsRequired();
        builder.Property(r => r.UpdatedAtUtc).HasColumnName("updated_at_utc");
        builder.Property(r => r.DeletedAtUtc).HasColumnName("deleted_at_utc");
        builder.Property(r => r.IsDeleted).HasColumnName("is_deleted").HasDefaultValue(false);
    }
}
