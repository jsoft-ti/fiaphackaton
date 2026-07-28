namespace DonationService.Domain.ReadModels;

/// <summary>
/// Storage-agnostic shape of a donation as materialized on the read side
/// (MongoDB). Deliberately a plain record (not a <c>BaseEntity</c> subtype)
/// since it represents a projection, not a transactional aggregate -
/// consumers only ever replace it wholesale (idempotent upsert).
/// </summary>
public sealed record DonationReadModel(
    Guid Id,
    Guid CampaignId,
    Guid UserId,
    string UserName,
    string UserEmail,
    decimal Value,
    string Currency,
    string PaymentMethod,
    DateTime DonationDate,
    string Status,
    DateTime CreatedAtUtc,
    Guid CorrelationId,
    Guid EventId);
