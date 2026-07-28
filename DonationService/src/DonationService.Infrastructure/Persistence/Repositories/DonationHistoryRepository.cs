using DonationService.Domain.Entities;
using DonationService.Domain.Repositories;

namespace DonationService.Infrastructure.Persistence.Repositories;

public sealed class DonationHistoryRepository : IDonationHistoryRepository
{
    private readonly DonationDbContext _dbContext;

    public DonationHistoryRepository(DonationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public void Add(DonationHistory history) => _dbContext.DonationHistories.Add(history);
}
