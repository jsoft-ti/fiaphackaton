using DonationService.Domain.ReadModels;

namespace DonationService.Domain.Repositories;

/// <summary>
/// Read-side (MongoDB) repository. Queries hit this repository directly
/// (CQRS: the read model is populated asynchronously by the Worker after
/// consuming <c>DonationCreatedEvent</c>, so it is eventually consistent
/// with the write side by design).
/// </summary>
public interface IDonationReadRepository
{
    Task<DonationReadModel?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<(IReadOnlyList<DonationReadModel> Items, long TotalCount)> GetByUserIdAsync(
        Guid userId, int page, int pageSize, CancellationToken cancellationToken);

    Task<(IReadOnlyList<DonationReadModel> Items, long TotalCount)> GetByCampaignIdAsync(
        Guid campaignId, int page, int pageSize, CancellationToken cancellationToken);

    Task<bool> ExistsByEventIdAsync(Guid eventId, CancellationToken cancellationToken);

    /// <summary>Idempotent write: inserts the document if absent, otherwise leaves the existing one untouched.</summary>
    Task UpsertAsync(DonationReadModel donation, CancellationToken cancellationToken);
}
