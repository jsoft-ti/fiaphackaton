using DonationService.Domain.Entities;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace DonationService.Infrastructure.Persistence;

/// <summary>
/// PostgreSQL (Supabase) write-side context. Holds ONLY what DonationService
/// itself owns transactionally: the Donation write record, its audit trail,
/// its business-level event log, and MassTransit's own Entity Framework Bus
/// Outbox tables (InboxState/OutboxMessage/OutboxState). All donation
/// documents used for reads live in MongoDB - never queried through this
/// context.
/// </summary>
public sealed class DonationDbContext : DbContext
{
    public DonationDbContext(DbContextOptions<DonationDbContext> options) : base(options)
    {
    }

    public DbSet<Donation> Donations => Set<Donation>();

    public DbSet<DonationHistory> DonationHistories => Set<DonationHistory>();

    public DbSet<DonationEvent> DonationEvents => Set<DonationEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("donation_service");

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(DonationDbContext).Assembly);

        // MassTransit's transactional Entity Framework Bus Outbox tables.
        // AddOutboxMessageEntity/AddOutboxStateEntity back the producer-side
        // (Api) outbox; AddInboxStateEntity is provisioned too in case a
        // future consumer-side inbox is adopted, but is currently unused
        // (the Worker relies on EventId-based idempotency in MongoDB instead).
        modelBuilder.AddInboxStateEntity();
        modelBuilder.AddOutboxMessageEntity();
        modelBuilder.AddOutboxStateEntity();

        base.OnModelCreating(modelBuilder);
    }
}
