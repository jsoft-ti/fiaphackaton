using DonationService.Domain.ReadModels;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace DonationService.Infrastructure.Persistence.Mongo;

/// <summary>
/// BSON-mapped persistence model for the "donations" MongoDB collection.
/// Kept separate from <see cref="DonationReadModel"/> (the Domain-facing,
/// storage-agnostic read shape) so that MongoDB.Driver attributes never leak
/// into the Domain layer.
/// </summary>
public sealed class MongoDonationDocument
{
    [BsonId]
    public Guid Id { get; set; }

    [BsonElement("campaignId")]
    public Guid CampaignId { get; set; }

    [BsonElement("userId")]
    public Guid UserId { get; set; }

    [BsonElement("userName")]
    public string UserName { get; set; } = string.Empty;

    [BsonElement("userEmail")]
    public string UserEmail { get; set; } = string.Empty;

    [BsonElement("value")]
    [BsonRepresentation(BsonType.Decimal128)]
    public decimal Value { get; set; }

    [BsonElement("currency")]
    public string Currency { get; set; } = string.Empty;

    [BsonElement("paymentMethod")]
    public string PaymentMethod { get; set; } = string.Empty;

    [BsonElement("donationDate")]
    public DateTime DonationDate { get; set; }

    [BsonElement("status")]
    public string Status { get; set; } = string.Empty;

    [BsonElement("createdAtUtc")]
    public DateTime CreatedAtUtc { get; set; }

    [BsonElement("correlationId")]
    public Guid CorrelationId { get; set; }

    [BsonElement("eventId")]
    public Guid EventId { get; set; }

    public static MongoDonationDocument FromReadModel(DonationReadModel model) => new()
    {
        Id = model.Id,
        CampaignId = model.CampaignId,
        UserId = model.UserId,
        UserName = model.UserName,
        UserEmail = model.UserEmail,
        Value = model.Value,
        Currency = model.Currency,
        PaymentMethod = model.PaymentMethod,
        DonationDate = model.DonationDate,
        Status = model.Status,
        CreatedAtUtc = model.CreatedAtUtc,
        CorrelationId = model.CorrelationId,
        EventId = model.EventId,
    };

    public DonationReadModel ToReadModel() => new(
        Id,
        CampaignId,
        UserId,
        UserName,
        UserEmail,
        Value,
        Currency,
        PaymentMethod,
        DonationDate,
        Status,
        CreatedAtUtc,
        CorrelationId,
        EventId);
}
