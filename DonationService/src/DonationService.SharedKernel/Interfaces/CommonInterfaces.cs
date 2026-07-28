namespace DonationService.SharedKernel.Interfaces;

public interface IDateTimeProvider
{
    DateTime UtcNow { get; }
}

/// <summary>
/// Exposes the identity of the authenticated caller, extracted from the JWT
/// issued by CampaignUserService (DonationService performs no authentication
/// of its own - it only trusts and validates that token).
/// </summary>
public interface ICurrentUserService
{
    Guid? UserId { get; }

    string? Email { get; }

    string? Name { get; }

    string? Role { get; }

    bool IsAuthenticated { get; }

    string? IpAddress { get; }

    string? UserAgent { get; }

    string CorrelationId { get; }
}
