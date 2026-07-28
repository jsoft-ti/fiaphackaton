using CampaignUserService.Domain.Entities;
using CampaignUserService.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace CampaignUserService.Infrastructure.Persistence.Repositories;

public sealed class RefreshTokenRepository(ApplicationDbContext dbContext) : IRefreshTokenRepository
{
    public Task<RefreshToken?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken) =>
        dbContext.RefreshTokens.FirstOrDefaultAsync(rt => rt.TokenHash == tokenHash, cancellationToken);

    public async Task<IReadOnlyList<RefreshToken>> GetActiveByUserIdAsync(Guid userId, CancellationToken cancellationToken) =>
        await dbContext.RefreshTokens
            .Where(rt => rt.UserId == userId && rt.RevokedAtUtc == null)
            .ToListAsync(cancellationToken);

    public void Add(RefreshToken refreshToken) => dbContext.RefreshTokens.Add(refreshToken);

    public async Task RevokeAllActiveForUserAsync(
        Guid userId,
        DateTime utcNow,
        string revokedByIp,
        CancellationToken cancellationToken)
    {
        var activeTokens = await dbContext.RefreshTokens
            .Where(rt => rt.UserId == userId && rt.RevokedAtUtc == null && rt.ExpiresAtUtc > utcNow)
            .ToListAsync(cancellationToken);

        foreach (var token in activeTokens)
        {
            token.Revoke(utcNow, revokedByIp);
        }
    }
}
