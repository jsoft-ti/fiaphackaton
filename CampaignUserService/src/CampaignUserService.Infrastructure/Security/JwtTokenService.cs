using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using CampaignUserService.Application.Common.Interfaces;
using CampaignUserService.Application.Common.Models;
using CampaignUserService.Domain.Entities;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace CampaignUserService.Infrastructure.Security;

/// <summary>
/// Generates and validates JWT access tokens and opaque refresh tokens
/// according to the "Jwt" configuration section (Options Pattern).
/// </summary>
public sealed class JwtTokenService(IOptions<JwtSettings> jwtOptions) : IJwtTokenService
{
    private readonly JwtSettings _settings = jwtOptions.Value;

    public TimeSpan AccessTokenLifetime => TimeSpan.FromMinutes(_settings.AccessTokenExpirationMinutes);

    public TimeSpan RefreshTokenLifetime => TimeSpan.FromDays(_settings.RefreshTokenExpirationDays);

    public AccessTokenResult GenerateAccessToken(User user, string roleName)
    {
        var jti = Guid.NewGuid().ToString();
        var expiresAtUtc = DateTime.UtcNow.Add(AccessTokenLifetime);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new("uid", user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(JwtRegisteredClaimNames.Jti, jti),
            new(ClaimTypes.Name, user.FullName),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Role, roleName)
        };

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.Secret));
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _settings.Issuer,
            audience: _settings.Audience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: expiresAtUtc,
            signingCredentials: credentials);

        var tokenValue = new JwtSecurityTokenHandler().WriteToken(token);

        return new AccessTokenResult(tokenValue, jti, expiresAtUtc);
    }

    public string GenerateRefreshTokenValue()
    {
        var randomBytes = RandomNumberGenerator.GetBytes(64);
        return Convert.ToBase64String(randomBytes)
            .Replace("+", "-")
            .Replace("/", "_")
            .Replace("=", string.Empty);
    }

    public string HashRefreshToken(string rawToken)
    {
        var bytes = Encoding.UTF8.GetBytes(rawToken);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash);
    }
}
