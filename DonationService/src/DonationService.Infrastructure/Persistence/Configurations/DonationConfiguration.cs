using DonationService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DonationService.Infrastructure.Persistence.Configurations;

public sealed class DonationConfiguration : IEntityTypeConfiguration<Donation>
{
    public void Configure(EntityTypeBuilder<Donation> builder)
    {
        builder.ToTable("donations");

        builder.HasKey(d => d.Id);

        builder.Property(d => d.Id).ValueGeneratedNever();

        builder.Property(d => d.CampaignId).IsRequired();

        builder.Property(d => d.UserId).IsRequired();

        builder.Property(d => d.UserName).IsRequired().HasMaxLength(200);

        builder.Property(d => d.UserEmail).IsRequired().HasMaxLength(320);

        builder.Property(d => d.Value).IsRequired().HasColumnType("numeric(18,2)");

        builder.Property(d => d.Currency).IsRequired().HasConversion<string>().HasMaxLength(10);

        builder.Property(d => d.PaymentMethod).IsRequired().HasConversion<string>().HasMaxLength(20);

        builder.Property(d => d.DonationDate).IsRequired();

        builder.Property(d => d.Status).IsRequired().HasConversion<string>().HasMaxLength(20);

        builder.Property(d => d.CorrelationId).IsRequired();

        builder.Property(d => d.EventId).IsRequired();

        builder.Property(d => d.CreatedAtUtc).IsRequired();

        builder.Property(d => d.UpdatedAtUtc);

        builder.HasIndex(d => d.CampaignId);
        builder.HasIndex(d => d.UserId);
        builder.HasIndex(d => d.EventId).IsUnique();
        builder.HasIndex(d => d.CorrelationId);
    }
}
