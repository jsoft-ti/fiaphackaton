using DonationService.Domain.Repositories;
using DonationService.Infrastructure.Persistence.Repositories;

namespace DonationService.Infrastructure.Persistence;

public sealed class UnitOfWork : IUnitOfWork
{
    private readonly DonationDbContext _dbContext;

    public UnitOfWork(DonationDbContext dbContext)
    {
        _dbContext = dbContext;
        Donations = new DonationRepository(dbContext);
        DonationEvents = new DonationEventRepository(dbContext);
        DonationHistories = new DonationHistoryRepository(dbContext);
    }

    public IDonationRepository Donations { get; }

    public IDonationEventRepository DonationEvents { get; }

    public IDonationHistoryRepository DonationHistories { get; }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken) =>
        _dbContext.SaveChangesAsync(cancellationToken);
}
