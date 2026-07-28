namespace DonationService.Contracts.Events.V1;

/// <summary>
/// Published by DonationService.Api (through the transactional outbox) after
/// a donation request has been validated and persisted, and consumed by
/// DonationService.Worker to materialize the donation document in MongoDB.
///
/// Versioning convention: this contract lives under the "V1" namespace.
/// Backward-incompatible changes must be introduced as a new
/// "DonationService.Contracts.Events.V2.DonationCreatedEvent" type (and a
/// new routing key / exchange binding) rather than mutating this record, so
/// existing consumers keep working unmodified against V1 messages already
/// in flight or stored in the DLQ.
/// </summary>
public sealed record DonationCreatedEvent(
    Guid EventId,
    Guid CorrelationId,
    Guid DonationId,
    Guid CampaignId,
    Guid UserId,
    string UserName,
    string UserEmail,
    decimal Value,
    string Currency,
    string PaymentMethod,
    DateTime DonationDate,
    DateTime CreatedAt) : IIntegrationEvent;
