using CampaignUserService.Domain.Entities;

namespace CampaignUserService.Domain.Repositories;

public interface IRefreshTokenRepository
{
    Task<RefreshToken?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken);

    Task<IReadOnlyList<RefreshToken>> GetActiveByUserIdAsync(Guid userId, CancellationToken cancellationToken);

    void Add(RefreshToken refreshToken);

    Task RevokeAllActiveForUserAsync(Guid userId, DateTime utcNow, string revokedByIp, CancellationToken cancellationToken);
}
