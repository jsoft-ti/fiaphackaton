using System.ComponentModel.DataAnnotations;

namespace DonationService.Infrastructure.ExternalServices;

public sealed class CampaignServiceOptions
{
    public const string SectionName = "CampaignService";

    [Required]
    public string BaseUrl { get; init; } = string.Empty;

    /// <summary>Relative route template; "{campaignId}" is replaced with the actual id.</summary>
    public string ValidationEndpointTemplate { get; init; } = "api/v1/campaigns/{campaignId}";

    public int TimeoutSeconds { get; init; } = 10;

    public int RetryCount { get; init; } = 3;

    public int CircuitBreakerFailureThreshold { get; init; } = 5;

    public int CircuitBreakerDurationSeconds { get; init; } = 30;
}
