using DonationService.SharedKernel.Common;

namespace DonationService.Domain.Entities;

/// <summary>
/// Business-level audit record of an integration event raised for a
/// donation (e.g. "DonationCreatedEvent v1"). This is distinct from
/// MassTransit's own internal Entity Framework outbox tables
/// (InboxState/OutboxMessage/OutboxState, configured separately on
/// <c>DonationDbContext</c>) which are the actual delivery mechanism -
/// this entity exists purely for domain-level observability/auditing
/// ("which events were raised for which donation, and when").
/// </summary>
public class DonationEvent : BaseEntity
{
    protected DonationEvent()
    {
    }

    private DonationEvent(
        Guid donationId,
        Guid eventId,
        Guid correlationId,
        string eventType,
        string payloadJson)
    {
        DonationId = donationId;
        EventId = eventId;
        CorrelationId = correlationId;
        EventType = eventType;
        PayloadJson = payloadJson;
        OccurredAtUtc = DateTime.UtcNow;
    }

    public Guid DonationId { get; private set; }

    public Donation? Donation { get; private set; }

    public Guid EventId { get; private set; }

    public Guid CorrelationId { get; private set; }

    /// <summary>Fully-qualified, versioned event type name, e.g. "DonationService.Contracts.Events.V1.DonationCreatedEvent".</summary>
    public string EventType { get; private set; } = string.Empty;

    /// <summary>JSON snapshot of the event payload at the moment it was raised, for auditing/troubleshooting.</summary>
    public string PayloadJson { get; private set; } = string.Empty;

    public DateTime OccurredAtUtc { get; private set; }

    public static DonationEvent Create(
        Guid donationId,
        Guid eventId,
        Guid correlationId,
        string eventType,
        string payloadJson) =>
        new(donationId, eventId, correlationId, eventType, payloadJson);
}
