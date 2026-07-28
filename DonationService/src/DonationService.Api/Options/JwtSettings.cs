namespace DonationService.Api.Options;

/// <summary>
/// DonationService never issues tokens - it only validates ones issued by
/// CampaignUserService. These settings must match CampaignUserService's
/// Issuer/Audience/SecretKey exactly, or every request will fail JWT
/// signature validation.
/// </summary>
public sealed class JwtSettings
{
    public const string SectionName = "Jwt";

    public string Issuer { get; init; } = string.Empty;

    public string Audience { get; init; } = string.Empty;

    public string SecretKey { get; init; } = string.Empty;

    public int ClockSkewSeconds { get; init; } = 60;
}
