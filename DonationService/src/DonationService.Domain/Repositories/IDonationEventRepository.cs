using DonationService.Domain.Entities;

namespace DonationService.Domain.Repositories;

public interface IDonationEventRepository
{
    void Add(DonationEvent donationEvent);
}
