using CampaignUserService.SharedKernel.Common;

namespace CampaignUserService.Domain.Entities;

/// <summary>
/// Single-use token issued when a user requests a password reset.
/// The raw token is emailed to the user (future SMTP integration);
/// only its hash is persisted.
/// </summary>
public class PasswordResetToken : BaseEntity
{
    protected PasswordResetToken()
    {
    }

    private PasswordResetToken(Guid userId, string tokenHash, DateTime expiresAtUtc)
    {
        UserId = userId;
        TokenHash = tokenHash;
        ExpiresAtUtc = expiresAtUtc;
    }

    public Guid UserId { get; private set; }

    public User? User { get; private set; }

    public string TokenHash { get; private set; } = string.Empty;

    public DateTime ExpiresAtUtc { get; private set; }

    public DateTime? UsedAtUtc { get; private set; }

    public bool IsUsed => UsedAtUtc is not null;

    public bool IsExpired(DateTime utcNow) => utcNow >= ExpiresAtUtc;

    public bool IsValid(DateTime utcNow) => !IsUsed && !IsExpired(utcNow);

    public static PasswordResetToken Create(Guid userId, string tokenHash, DateTime expiresAtUtc) =>
        new(userId, tokenHash, expiresAtUtc);

    public void MarkUsed(DateTime utcNow)
    {
        UsedAtUtc = utcNow;
        MarkUpdated(utcNow);
    }
}
