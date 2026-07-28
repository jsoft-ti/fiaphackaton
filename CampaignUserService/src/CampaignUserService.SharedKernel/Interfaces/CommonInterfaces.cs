namespace CampaignUserService.SharedKernel.Interfaces;

/// <summary>
/// Abstraction over "now", so handlers/tests never call <see cref="DateTime.UtcNow"/> directly.
/// </summary>
public interface IDateTimeProvider
{
    DateTime UtcNow { get; }
}

/// <summary>
/// Exposes the identity of the currently authenticated caller, resolved from
/// the JWT claims by the Api layer and consumed by Application/Infrastructure.
/// </summary>
public interface ICurrentUserService
{
    Guid? UserId { get; }

    string? Email { get; }

    string? Role { get; }

    bool IsAuthenticated { get; }

    string? IpAddress { get; }

    string? UserAgent { get; }
}
