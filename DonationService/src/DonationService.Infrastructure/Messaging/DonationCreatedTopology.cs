namespace DonationService.Infrastructure.Messaging;

/// <summary>
/// Shared RabbitMQ topology constants for <c>DonationCreatedEvent</c>, used
/// identically by both the producer (Api) and the consumer (Worker) bus
/// configuration so the exchange/routing key never drift out of sync
/// between the two hosts.
/// </summary>
public static class DonationCreatedTopology
{
    public const string ExchangeName = "donation-service.donation-created.v1";

    public const string RoutingKey = "donation.created.v1";

    /// <summary>
    /// Must match exactly on both sides of the bus: the producer's message
    /// topology (which declares this exchange implicitly via
    /// <c>cfg.Publish&lt;DonationCreatedEvent&gt;</c>) and the consumer's
    /// explicit <c>e.Bind(...)</c> both declare the SAME exchange name, and
    /// RabbitMQ/MassTransit requires every declaration of a given exchange
    /// to agree on its type. MassTransit's default exchange type is
    /// "fanout" - routing-key-based delivery requires "direct" instead, and
    /// leaving one side on the default caused a real
    /// MassTransit.ConfigurationException ("entity settings did not match
    /// the existing entity") at Worker startup.
    /// </summary>
    public const string ExchangeType = "direct";
}
