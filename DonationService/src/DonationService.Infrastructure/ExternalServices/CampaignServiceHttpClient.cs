using System.Net;
using System.Net.Http.Json;
using DonationService.Application.Common.Interfaces;
using DonationService.SharedKernel.Exceptions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DonationService.Infrastructure.ExternalServices;

/// <summary>
/// HTTP implementation of <see cref="ICampaignServiceClient"/>. Registered
/// via <c>AddHttpClient&lt;CampaignServiceHttpClient&gt;</c> with a Polly
/// retry + circuit-breaker policy chain (see
/// <c>DependencyInjection.AddCampaignServiceClient</c>), so transient
/// network/5xx failures are retried automatically before this class ever
/// sees an exception.
/// </summary>
public sealed class CampaignServiceHttpClient : ICampaignServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly CampaignServiceOptions _options;
    private readonly ILogger<CampaignServiceHttpClient> _logger;

    public CampaignServiceHttpClient(
        HttpClient httpClient,
        IOptions<CampaignServiceOptions> options,
        ILogger<CampaignServiceHttpClient> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<CampaignValidationResult> ValidateCampaignAsync(Guid campaignId, CancellationToken cancellationToken)
    {
        var route = _options.ValidationEndpointTemplate.Replace("{campaignId}", campaignId.ToString());

        try
        {
            var response = await _httpClient.GetAsync(route, cancellationToken);

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return CampaignValidationResult.NotFound();
            }

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError(
                    "CampaignService returned {StatusCode} while validating campaign {CampaignId}",
                    response.StatusCode,
                    campaignId);

                throw new UpstreamDependencyException(
                    $"CampaignService returned an unexpected status code ({(int)response.StatusCode}) while validating campaign '{campaignId}'.");
            }

            var payload = await response.Content.ReadFromJsonAsync<CampaignServiceResponse>(cancellationToken: cancellationToken);

            if (payload is null)
            {
                throw new UpstreamDependencyException(
                    $"CampaignService returned an empty payload while validating campaign '{campaignId}'.");
            }

            return new CampaignValidationResult(
                Exists: true,
                IsActive: payload.IsActive,
                AcceptsDonations: payload.AcceptsDonations,
                CampaignName: payload.Name);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to reach CampaignService while validating campaign {CampaignId}", campaignId);
            throw new UpstreamDependencyException(
                $"CampaignService is unreachable while validating campaign '{campaignId}'.", ex);
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogError(ex, "CampaignService call timed out while validating campaign {CampaignId}", campaignId);
            throw new UpstreamDependencyException(
                $"CampaignService timed out while validating campaign '{campaignId}'.", ex);
        }
    }

    /// <summary>Shape expected from CampaignUserService/CampaignService's campaign lookup endpoint.</summary>
    private sealed record CampaignServiceResponse(
        Guid Id,
        string Name,
        bool IsActive,
        bool AcceptsDonations);
}
