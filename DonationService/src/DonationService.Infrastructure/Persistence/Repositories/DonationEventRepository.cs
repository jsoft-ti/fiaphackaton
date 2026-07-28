using DonationService.Domain.Entities;
using DonationService.Domain.Repositories;

namespace DonationService.Infrastructure.Persistence.Repositories;

public sealed class DonationEventRepository : IDonationEventRepository
{
    private readonly DonationDbContext _dbContext;

    public DonationEventRepository(DonationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public void Add(DonationEvent donationEvent) => _dbContext.DonationEvents.Add(donationEvent);
}
