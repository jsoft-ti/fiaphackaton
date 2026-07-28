using CampaignUserService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CampaignUserService.Infrastructure.Persistence.Configurations;

public sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("refresh_tokens");

        builder.HasKey(rt => rt.Id);
        builder.Property(rt => rt.Id).HasColumnName("id").ValueGeneratedNever();

        builder.Property(rt => rt.UserId).HasColumnName("user_id").IsRequired();

        builder.Property(rt => rt.TokenHash).HasColumnName("token_hash").HasMaxLength(500).IsRequired();
        builder.HasIndex(rt => rt.TokenHash).IsUnique().HasDatabaseName("ix_refresh_tokens_token_hash");

        builder.Property(rt => rt.ExpiresAtUtc).HasColumnName("expires_at_utc").IsRequired();
        builder.Property(rt => rt.RevokedAtUtc).HasColumnName("revoked_at_utc");
        builder.Property(rt => rt.RevokedByIp).HasColumnName("revoked_by_ip").HasMaxLength(64);
        builder.Property(rt => rt.ReplacedByTokenHash).HasColumnName("replaced_by_token_hash").HasMaxLength(500);
        builder.Property(rt => rt.CreatedByIp).HasColumnName("created_by_ip").HasMaxLength(64).IsRequired();
        builder.Property(rt => rt.UserAgent).HasColumnName("user_agent").HasMaxLength(512);

        builder.HasOne(rt => rt.User)
            .WithMany()
            .HasForeignKey(rt => rt.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(rt => rt.UserId).HasDatabaseName("ix_refresh_tokens_user_id");

        builder.Property(rt => rt.CreatedAtUtc).HasColumnName("created_at_utc").IsRequired();
        builder.Property(rt => rt.UpdatedAtUtc).HasColumnName("updated_at_utc");
        builder.Property(rt => rt.DeletedAtUtc).HasColumnName("deleted_at_utc");
        builder.Property(rt => rt.IsDeleted).HasColumnName("is_deleted").HasDefaultValue(false);
    }
}
