namespace DonationService.Contracts;

/// <summary>
/// Common contract every DonationService integration event implements.
/// Kept intentionally minimal (no base class) so records stay simple,
/// serializable POCOs - MassTransit does not require any base type.
/// </summary>
public interface IIntegrationEvent
{
    /// <summary>Unique id of this specific event instance (idempotency key for consumers).</summary>
    Guid EventId { get; }

    /// <summary>Correlates this event back to the originating HTTP request/trace across services.</summary>
    Guid CorrelationId { get; }

    DateTime CreatedAt { get; }
}
