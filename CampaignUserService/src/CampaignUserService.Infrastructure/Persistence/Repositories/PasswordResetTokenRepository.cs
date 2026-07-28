using CampaignUserService.Domain.Entities;
using CampaignUserService.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace CampaignUserService.Infrastructure.Persistence.Repositories;

public sealed class PasswordResetTokenRepository(ApplicationDbContext dbContext) : IPasswordResetTokenRepository
{
    public Task<PasswordResetToken?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken) =>
        dbContext.PasswordResetTokens.FirstOrDefaultAsync(t => t.TokenHash == tokenHash, cancellationToken);

    public async Task InvalidateActiveTokensForUserAsync(Guid userId, CancellationToken cancellationToken)
    {
        var utcNow = DateTime.UtcNow;

        var activeTokens = await dbContext.PasswordResetTokens
            .Where(t => t.UserId == userId && t.UsedAtUtc == null && t.ExpiresAtUtc > utcNow)
            .ToListAsync(cancellationToken);

        foreach (var token in activeTokens)
        {
            token.MarkUsed(utcNow);
        }
    }

    public void Add(PasswordResetToken token) => dbContext.PasswordResetTokens.Add(token);
}
