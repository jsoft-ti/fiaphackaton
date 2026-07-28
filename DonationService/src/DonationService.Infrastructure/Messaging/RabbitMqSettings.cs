using System.ComponentModel.DataAnnotations;

namespace DonationService.Infrastructure.Messaging;

public sealed class RabbitMqSettings
{
    public const string SectionName = "RabbitMq";

    [Required]
    public string Host { get; init; } = "localhost";

    public ushort Port { get; init; } = 5672;

    public string VirtualHost { get; init; } = "/";

    [Required]
    public string Username { get; init; } = "guest";

    [Required]
    public string Password { get; init; } = "guest";

    /// <summary>Base name for the exchange/queue pair backing DonationCreatedEvent (e.g. "donation-created").</summary>
    public string DonationCreatedQueueName { get; init; } = "donation-created-queue";

    public ushort PrefetchCount { get; init; } = 16;

    public int ConcurrencyLimit { get; init; } = 8;

    public int RetryCount { get; init; } = 5;

    public int RetryIntervalSeconds { get; init; } = 5;
}
