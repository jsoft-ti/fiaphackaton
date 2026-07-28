using CampaignUserService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CampaignUserService.Infrastructure.Persistence.Configurations;

public sealed class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("audit_logs");

        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).HasColumnName("id").ValueGeneratedNever();

        builder.Property(a => a.UserId).HasColumnName("user_id");

        builder.Property(a => a.Action).HasColumnName("action").HasConversion<string>().HasMaxLength(50).IsRequired();

        builder.Property(a => a.Description).HasColumnName("description").HasMaxLength(1000).IsRequired();

        builder.Property(a => a.IpAddress).HasColumnName("ip_address").HasMaxLength(64);
        builder.Property(a => a.UserAgent).HasColumnName("user_agent").HasMaxLength(512);

        builder.Property(a => a.OccurredAtUtc).HasColumnName("occurred_at_utc").IsRequired();

        builder.HasOne(a => a.User)
            .WithMany()
            .HasForeignKey(a => a.UserId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(a => a.UserId).HasDatabaseName("ix_audit_logs_user_id");
        builder.HasIndex(a => a.OccurredAtUtc).HasDatabaseName("ix_audit_logs_occurred_at_utc");

        builder.Property(a => a.CreatedAtUtc).HasColumnName("created_at_utc").IsRequired();
        builder.Property(a => a.UpdatedAtUtc).HasColumnName("updated_at_utc");
        builder.Property(a => a.DeletedAtUtc).HasColumnName("deleted_at_utc");
        builder.Property(a => a.IsDeleted).HasColumnName("is_deleted").HasDefaultValue(false);
    }
}
