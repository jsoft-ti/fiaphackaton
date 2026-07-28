namespace CampaignUserService.Application.Common.Models;

/// <summary>
/// Strongly typed representation of the "Jwt" configuration section
/// (Options Pattern). Bound and validated at startup.
/// </summary>
public sealed class JwtSettings
{
    public const string SectionName = "Jwt";

    public string Secret { get; init; } = string.Empty;

    public string Issuer { get; init; } = string.Empty;

    public string Audience { get; init; } = string.Empty;

    public int AccessTokenExpirationMinutes { get; init; } = 15;

    public int RefreshTokenExpirationDays { get; init; } = 7;
}
