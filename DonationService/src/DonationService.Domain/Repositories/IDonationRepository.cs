using DonationService.Domain.Entities;

namespace DonationService.Domain.Repositories;

/// <summary>Write-side (PostgreSQL) repository for the transactional donation-request record.</summary>
public interface IDonationRepository
{
    Task<Donation?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    void Add(Donation donation);
}
