using CampaignUserService.SharedKernel.Common;

namespace CampaignUserService.Domain.Entities;

/// <summary>
/// A long-lived opaque token used to obtain new access tokens without
/// forcing the user to re-authenticate. Supports rotation and revocation
/// (acts as the persisted side of the refresh-token blacklist).
/// </summary>
public class RefreshToken : BaseEntity
{
    protected RefreshToken()
    {
    }

    private RefreshToken(
        Guid userId,
        string tokenHash,
        DateTime expiresAtUtc,
        string createdByIp,
        string? userAgent)
    {
        UserId = userId;
        TokenHash = tokenHash;
        ExpiresAtUtc = expiresAtUtc;
        CreatedByIp = createdByIp;
        UserAgent = userAgent;
    }

    public Guid UserId { get; private set; }

    public User? User { get; private set; }

    /// <summary>SHA-256 hash of the raw refresh token. The raw value is never persisted.</summary>
    public string TokenHash { get; private set; } = string.Empty;

    public DateTime ExpiresAtUtc { get; private set; }

    public DateTime? RevokedAtUtc { get; private set; }

    public string? RevokedByIp { get; private set; }

    /// <summary>When set, points to the token that replaced this one (rotation chain).</summary>
    public string? ReplacedByTokenHash { get; private set; }

    public string CreatedByIp { get; private set; } = string.Empty;

    public string? UserAgent { get; private set; }

    public bool IsRevoked => RevokedAtUtc is not null;

    public bool IsExpired(DateTime utcNow) => utcNow >= ExpiresAtUtc;

    public bool IsActive(DateTime utcNow) => !IsRevoked && !IsExpired(utcNow);

    public static RefreshToken Create(
        Guid userId,
        string tokenHash,
        DateTime expiresAtUtc,
        string createdByIp,
        string? userAgent) =>
        new(userId, tokenHash, expiresAtUtc, createdByIp, userAgent);

    public void Revoke(DateTime utcNow, string revokedByIp, string? replacedByTokenHash = null)
    {
        RevokedAtUtc = utcNow;
        RevokedByIp = revokedByIp;
        ReplacedByTokenHash = replacedByTokenHash;
        MarkUpdated(utcNow);
    }
}
