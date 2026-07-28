using CampaignUserService.Domain.Entities;

namespace CampaignUserService.Domain.Repositories;

public interface IPasswordResetTokenRepository
{
    Task<PasswordResetToken?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken);

    Task InvalidateActiveTokensForUserAsync(Guid userId, CancellationToken cancellationToken);

    void Add(PasswordResetToken token);
}
