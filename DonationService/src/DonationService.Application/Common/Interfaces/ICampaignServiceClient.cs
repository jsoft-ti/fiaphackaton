namespace DonationService.Application.Common.Interfaces;

/// <summary>
/// Domain-facing abstraction (Gateway pattern) over CampaignService. The
/// Application layer depends only on this interface; the actual HTTP
/// call (HttpClientFactory + Polly retry/circuit-breaker) is implemented
/// in Infrastructure, so the domain/application never couples to HTTP
/// concerns directly.
/// </summary>
public interface ICampaignServiceClient
{
    Task<CampaignValidationResult> ValidateCampaignAsync(Guid campaignId, CancellationToken cancellationToken);
}

public sealed record CampaignValidationResult(
    bool Exists,
    bool IsActive,
    bool AcceptsDonations,
    string? CampaignName)
{
    public static CampaignValidationResult NotFound() => new(false, false, false, null);
}
