using DonationService.Domain.Entities;

namespace DonationService.Domain.Repositories;

public interface IDonationHistoryRepository
{
    void Add(DonationHistory history);
}
