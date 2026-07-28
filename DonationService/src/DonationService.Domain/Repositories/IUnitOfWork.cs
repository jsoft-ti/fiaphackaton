namespace DonationService.Domain.Repositories;

/// <summary>
/// Coordinates the PostgreSQL write-side persistence. <see cref="SaveChangesAsync"/>
/// is where the MassTransit Entity Framework Bus Outbox intercepts and
/// persists any messages published within the same scope, guaranteeing the
/// donation record and its outbox message commit atomically.
/// </summary>
public interface IUnitOfWork
{
    IDonationRepository Donations { get; }

    IDonationEventRepository DonationEvents { get; }

    IDonationHistoryRepository DonationHistories { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
