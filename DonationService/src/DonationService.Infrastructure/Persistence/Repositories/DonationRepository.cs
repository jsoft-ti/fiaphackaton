using DonationService.Domain.Entities;
using DonationService.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace DonationService.Infrastructure.Persistence.Repositories;

public sealed class DonationRepository : IDonationRepository
{
    private readonly DonationDbContext _dbContext;

    public DonationRepository(DonationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<Donation?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        _dbContext.Donations.FirstOrDefaultAsync(d => d.Id == id, cancellationToken);

    public void Add(Donation donation) => _dbContext.Donations.Add(donation);
}
