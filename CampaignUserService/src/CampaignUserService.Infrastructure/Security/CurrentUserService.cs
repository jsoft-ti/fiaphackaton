using System.Security.Claims;
using CampaignUserService.SharedKernel.Interfaces;
using Microsoft.AspNetCore.Http;

namespace CampaignUserService.Infrastructure.Security;

/// <summary>
/// Resolves the current authenticated user from the ambient HttpContext.
/// Consumed by the Application layer through the <see cref="ICurrentUserService"/>
/// abstraction so it never depends on ASP.NET Core directly.
/// </summary>
public sealed class CurrentUserService(IHttpContextAccessor httpContextAccessor) : ICurrentUserService
{
    private HttpContext? Context => httpContextAccessor.HttpContext;

    public Guid? UserId
    {
        get
        {
            var value = Context?.User.FindFirstValue("uid") ?? Context?.User.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(value, out var id) ? id : null;
        }
    }

    public string? Email => Context?.User.FindFirstValue(ClaimTypes.Email);

    public string? Role => Context?.User.FindFirstValue(ClaimTypes.Role);

    public bool IsAuthenticated => Context?.User.Identity?.IsAuthenticated ?? false;

    public string? IpAddress
    {
        get
        {
            var forwardedFor = Context?.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(forwardedFor))
            {
                return forwardedFor.Split(',')[0].Trim();
            }

            return Context?.Connection.RemoteIpAddress?.ToString();
        }
    }

    public string? UserAgent => Context?.Request.Headers.UserAgent.FirstOrDefault();
}
