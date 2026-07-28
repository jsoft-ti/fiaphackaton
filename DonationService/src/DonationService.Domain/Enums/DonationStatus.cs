namespace DonationService.Domain.Enums;

/// <summary>
/// Lifecycle of a donation request on the write (PostgreSQL) side. The
/// MongoDB read-side document (materialized by the Worker) additionally
/// tracks <c>Confirmed</c> once the event has actually been consumed and
/// persisted there.
/// </summary>
public enum DonationStatus
{
    /// <summary>Persisted locally; outbox message not yet delivered to the broker.</summary>
    PendingPublish = 1,

    /// <summary>Outbox message successfully delivered to RabbitMQ.</summary>
    Published = 2,

    /// <summary>The Worker consumed the event and persisted the donation document in MongoDB.</summary>
    Confirmed = 3,

    /// <summary>Outbox delivery exhausted its retry budget (moved toward DLQ investigation).</summary>
    PublishFailed = 4
}
