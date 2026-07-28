using DonationService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DonationService.Infrastructure.Persistence.Configurations;

public sealed class DonationHistoryConfiguration : IEntityTypeConfiguration<DonationHistory>
{
    public void Configure(EntityTypeBuilder<DonationHistory> builder)
    {
        builder.ToTable("donation_histories");

        builder.HasKey(h => h.Id);

        builder.Property(h => h.Id).ValueGeneratedNever();

        builder.Property(h => h.DonationId).IsRequired();

        builder.Property(h => h.PreviousStatus).IsRequired().HasConversion<string>().HasMaxLength(20);

        builder.Property(h => h.NewStatus).IsRequired().HasConversion<string>().HasMaxLength(20);

        builder.Property(h => h.Description).IsRequired().HasMaxLength(500);

        builder.Property(h => h.OccurredAtUtc).IsRequired();

        builder.HasOne(h => h.Donation)
            .WithMany()
            .HasForeignKey(h => h.DonationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(h => h.DonationId);
    }
}
