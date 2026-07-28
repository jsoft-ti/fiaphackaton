using System.Security.Claims;
using DonationService.SharedKernel.Interfaces;
using Microsoft.AspNetCore.Http;

namespace DonationService.Infrastructure.Services;

/// <summary>
/// Extracts the caller's identity from the ASP.NET Core <see cref="HttpContext"/>
/// populated by JWT Bearer authentication - claims issued by CampaignUserService.
/// Multiple claim-type fallbacks are checked (short JWT names and the
/// long ClaimTypes URIs) because JwtBearer's inbound claim mapping behavior
/// depends on how the token was configured upstream.
/// </summary>
public sealed class CurrentUserService : ICurrentUserService
{
    private const string CorrelationIdHeader = "X-Correlation-Id";
    private const string CorrelationIdItemKey = "CorrelationId";

    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    private ClaimsPrincipal? User => _httpContextAccessor.HttpContext?.User;

    public bool IsAuthenticated => User?.Identity?.IsAuthenticated ?? false;

    public Guid? UserId
    {
        get
        {
            var raw = FindClaim(ClaimTypes.NameIdentifier, "sub", "userId");
            return Guid.TryParse(raw, out var id) ? id : null;
        }
    }

    public string? Email => FindClaim(ClaimTypes.Email, "email");

    public string? Name => FindClaim(ClaimTypes.Name, "name", "given_name");

    public string? Role => FindClaim(ClaimTypes.Role, "role");

    public string? IpAddress => _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString();

    public string? UserAgent => _httpContextAccessor.HttpContext?.Request.Headers.UserAgent.ToString();

    public string CorrelationId
    {
        get
        {
            var httpContext = _httpContextAccessor.HttpContext;

            if (httpContext is null)
            {
                return Guid.NewGuid().ToString();
            }

            if (httpContext.Items.TryGetValue(CorrelationIdItemKey, out var cached) && cached is string cachedValue)
            {
                return cachedValue;
            }

            var fromHeader = httpContext.Request.Headers[CorrelationIdHeader].FirstOrDefault();
            var correlationId = string.IsNullOrWhiteSpace(fromHeader) ? Guid.NewGuid().ToString() : fromHeader;

            httpContext.Items[CorrelationIdItemKey] = correlationId;

            return correlationId;
        }
    }

    private string? FindClaim(params string[] claimTypes)
    {
        var principal = User;

        if (principal is null)
        {
            return null;
        }

        foreach (var claimType in claimTypes)
        {
            var value = principal.FindFirst(claimType)?.Value;

            if (!string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
        }

        return null;
    }
}
