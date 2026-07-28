using DonationService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DonationService.Infrastructure.Persistence.Configurations;

public sealed class DonationEventConfiguration : IEntityTypeConfiguration<DonationEvent>
{
    public void Configure(EntityTypeBuilder<DonationEvent> builder)
    {
        builder.ToTable("donation_events");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id).ValueGeneratedNever();

        builder.Property(e => e.DonationId).IsRequired();

        builder.Property(e => e.EventId).IsRequired();

        builder.Property(e => e.CorrelationId).IsRequired();

        builder.Property(e => e.EventType).IsRequired().HasMaxLength(300);

        builder.Property(e => e.PayloadJson).IsRequired().HasColumnType("jsonb");

        builder.Property(e => e.OccurredAtUtc).IsRequired();

        builder.HasOne(e => e.Donation)
            .WithMany()
            .HasForeignKey(e => e.DonationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(e => e.EventId).IsUnique();
        builder.HasIndex(e => e.DonationId);
    }
}
