using CampaignUserService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CampaignUserService.Infrastructure.Persistence.Configurations;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");

        builder.HasKey(u => u.Id);

        builder.Property(u => u.Id).HasColumnName("id").ValueGeneratedNever();

        builder.Property(u => u.FirstName).HasColumnName("first_name").HasMaxLength(100).IsRequired();
        builder.Property(u => u.LastName).HasColumnName("last_name").HasMaxLength(100).IsRequired();

        builder.Property(u => u.Email).HasColumnName("email").HasMaxLength(256).IsRequired();
        builder.HasIndex(u => u.Email).IsUnique().HasDatabaseName("ix_users_email");

        builder.Property(u => u.PasswordHash).HasColumnName("password_hash").HasMaxLength(500).IsRequired();

        builder.Property(u => u.PhoneNumber).HasColumnName("phone_number").HasMaxLength(20);

        builder.Property(u => u.Cpf).HasColumnName("cpf").HasMaxLength(11);
        builder.HasIndex(u => u.Cpf).IsUnique().HasDatabaseName("ix_users_cpf").HasFilter("cpf IS NOT NULL");

        builder.Property(u => u.PhotoUrl).HasColumnName("photo_url").HasMaxLength(2048);

        builder.Property(u => u.BirthDate).HasColumnName("birth_date").HasColumnType("date");

        builder.Property(u => u.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(20).IsRequired();

        builder.Property(u => u.EmailConfirmed).HasColumnName("email_confirmed").HasDefaultValue(false);

        builder.Property(u => u.LastLoginAtUtc).HasColumnName("last_login_at_utc");

        builder.Property(u => u.AccessFailedCount).HasColumnName("access_failed_count").HasDefaultValue(0);

        builder.Property(u => u.CreatedAtUtc).HasColumnName("created_at_utc").IsRequired();
        builder.Property(u => u.UpdatedAtUtc).HasColumnName("updated_at_utc");
        builder.Property(u => u.DeletedAtUtc).HasColumnName("deleted_at_utc");
        builder.Property(u => u.IsDeleted).HasColumnName("is_deleted").HasDefaultValue(false);

        builder.HasMany(u => u.UserRoles)
            .WithOne(ur => ur.User)
            .HasForeignKey(ur => ur.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
