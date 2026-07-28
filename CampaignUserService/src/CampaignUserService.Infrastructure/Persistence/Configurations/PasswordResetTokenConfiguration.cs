using CampaignUserService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CampaignUserService.Infrastructure.Persistence.Configurations;

public sealed class PasswordResetTokenConfiguration : IEntityTypeConfiguration<PasswordResetToken>
{
    public void Configure(EntityTypeBuilder<PasswordResetToken> builder)
    {
        builder.ToTable("password_reset_tokens");

        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).HasColumnName("id").ValueGeneratedNever();

        builder.Property(t => t.UserId).HasColumnName("user_id").IsRequired();

        builder.Property(t => t.TokenHash).HasColumnName("token_hash").HasMaxLength(500).IsRequired();
        builder.HasIndex(t => t.TokenHash).IsUnique().HasDatabaseName("ix_password_reset_tokens_token_hash");

        builder.Property(t => t.ExpiresAtUtc).HasColumnName("expires_at_utc").IsRequired();
        builder.Property(t => t.UsedAtUtc).HasColumnName("used_at_utc");

        builder.HasOne(t => t.User)
            .WithMany()
            .HasForeignKey(t => t.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(t => t.UserId).HasDatabaseName("ix_password_reset_tokens_user_id");

        builder.Property(t => t.CreatedAtUtc).HasColumnName("created_at_utc").IsRequired();
        builder.Property(t => t.UpdatedAtUtc).HasColumnName("updated_at_utc");
        builder.Property(t => t.DeletedAtUtc).HasColumnName("deleted_at_utc");
        builder.Property(t => t.IsDeleted).HasColumnName("is_deleted").HasDefaultValue(false);
    }
}
