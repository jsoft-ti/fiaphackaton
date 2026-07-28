using DonationService.Application.Features.Donations.Commands.PersistDonation;
using DonationService.Contracts.Events.V1;
using DonationService.Infrastructure.Services;
using MassTransit;
using MediatR;
using Microsoft.Extensions.Logging;

namespace DonationService.Worker.Consumers;

/// <summary>
/// Consumes <c>DonationCreatedEvent</c> and materializes it as a document in
/// the MongoDB read model via <see cref="PersistDonationCommand"/>.
/// Idempotent (see <c>PersistDonationCommandHandler</c>): MassTransit's
/// at-least-once delivery means this may run more than once for the same
/// EventId. Throwing on failure lets MassTransit's configured retry policy
/// (see <c>DependencyInjection.AddDonationServiceConsumerMessaging</c>) take
/// over, and ultimately route the message to the RabbitMQ "_error" queue
/// (this service's DLQ) once retries are exhausted.
/// </summary>
public sealed class DonationCreatedConsumer : IConsumer<DonationCreatedEvent>
{
    private readonly ISender _sender;
    private readonly ILogger<DonationCreatedConsumer> _logger;

    public DonationCreatedConsumer(ISender sender, ILogger<DonationCreatedConsumer> logger)
    {
        _sender = sender;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<DonationCreatedEvent> context)
    {
        var message = context.Message;

        AmbientCorrelationCurrentUserService.SetCorrelationId(message.CorrelationId.ToString());

        _logger.LogInformation(
            "Consuming DonationCreatedEvent {EventId} for donation {DonationId} | CorrelationId={CorrelationId}",
            message.EventId,
            message.DonationId,
            message.CorrelationId);

        var command = new PersistDonationCommand(
            message.EventId,
            message.CorrelationId,
            message.DonationId,
            message.CampaignId,
            message.UserId,
            message.UserName,
            message.UserEmail,
            message.Value,
            message.Currency,
            message.PaymentMethod,
            message.DonationDate,
            message.CreatedAt);

        var result = await _sender.Send(command, context.CancellationToken);

        if (result.IsFailure)
        {
            _logger.LogError(
                "Failed to persist donation {DonationId} from event {EventId}: {ErrorCode} - {ErrorMessage}",
                message.DonationId,
                message.EventId,
                result.Error.Code,
                result.Error.Message);

            throw new InvalidOperationException(
                $"PersistDonationCommand failed for donation '{message.DonationId}': {result.Error.Code} - {result.Error.Message}");
        }
    }
}
