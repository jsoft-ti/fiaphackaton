using DonationService.SharedKernel.Interfaces;

namespace DonationService.Infrastructure.Services;

/// <summary>
/// <see cref="ICurrentUserService"/> implementation for hosts with no HTTP
/// request (i.e. DonationService.Worker). There is no authenticated
/// identity to expose on the consumer side - DonationService.Worker never
/// validates a JWT, it only consumes trusted internal events - so identity
/// members are all empty. <see cref="CorrelationId"/> instead flows the
/// CorrelationId carried by the consumed <c>DonationCreatedEvent</c>, set
/// via <see cref="SetCorrelationId"/> by the consumer before dispatching to
/// MediatR, so <c>LoggingBehavior</c> log lines stay traceable end to end.
/// </summary>
public sealed class AmbientCorrelationCurrentUserService : ICurrentUserService
{
    private static readonly AsyncLocal<string?> AmbientCorrelationId = new();

    public static void SetCorrelationId(string correlationId) => AmbientCorrelationId.Value = correlationId;

    public bool IsAuthenticated => false;

    public Guid? UserId => null;

    public string? Email => null;

    public string? Name => null;

    public string? Role => null;

    public string? IpAddress => null;

    public string? UserAgent => null;

    public string CorrelationId => AmbientCorrelationId.Value ?? Guid.NewGuid().ToString();
}
