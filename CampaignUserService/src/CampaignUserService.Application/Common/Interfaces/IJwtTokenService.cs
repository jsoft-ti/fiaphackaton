using CampaignUserService.Domain.Entities;

namespace CampaignUserService.Application.Common.Interfaces;

public sealed record AccessTokenResult(string Token, string Jti, DateTime ExpiresAtUtc);

public interface IJwtTokenService
{
    /// <summary>Generates a signed JWT access token embedding UserId, Email, Name, Role and Jti claims.</summary>
    AccessTokenResult GenerateAccessToken(User user, string roleName);

    /// <summary>Generates a cryptographically secure random raw refresh token (opaque, not a JWT).</summary>
    string GenerateRefreshTokenValue();

    /// <summary>Hashes a raw refresh token so only the hash is persisted at rest.</summary>
    string HashRefreshToken(string rawToken);

    TimeSpan AccessTokenLifetime { get; }

    TimeSpan RefreshTokenLifetime { get; }
}
