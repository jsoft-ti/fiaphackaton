using DonationService.Domain.ReadModels;
using DonationService.Domain.Repositories;
using DonationService.SharedKernel.Common;
using DonationService.SharedKernel.Errors;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;

namespace DonationService.Application.Features.Donations.Commands.PersistDonation;

/// <summary>
/// Consumer-side command: materializes a consumed <c>DonationCreatedEvent</c>
/// as a document in the MongoDB read model. Dispatched by
/// <c>DonationCreatedConsumer</c> in DonationService.Worker - never called
/// from the Api. Idempotent by <see cref="EventId"/>: RabbitMQ/MassTransit's
/// at-least-once delivery guarantee means this handler may run more than
/// once for the same event, and it must produce the same end state either way.
/// </summary>
public sealed record PersistDonationCommand(
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
    DateTime CreatedAt) : IRequest<Result>;

public sealed class PersistDonationCommandValidator : AbstractValidator<PersistDonationCommand>
{
    public PersistDonationCommandValidator()
    {
        RuleFor(x => x.EventId).NotEmpty();
        RuleFor(x => x.DonationId).NotEmpty();
        RuleFor(x => x.CampaignId).NotEmpty();
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.UserName).NotEmpty();
        RuleFor(x => x.UserEmail).NotEmpty();
        RuleFor(x => x.Value).GreaterThan(0m);
        RuleFor(x => x.Currency).NotEmpty();
        RuleFor(x => x.PaymentMethod).NotEmpty();
    }
}

public sealed class PersistDonationCommandHandler : IRequestHandler<PersistDonationCommand, Result>
{
    private readonly IDonationReadRepository _readRepository;
    private readonly ILogger<PersistDonationCommandHandler> _logger;

    public PersistDonationCommandHandler(
        IDonationReadRepository readRepository,
        ILogger<PersistDonationCommandHandler> logger)
    {
        _readRepository = readRepository;
        _logger = logger;
    }

    public async Task<Result> Handle(PersistDonationCommand request, CancellationToken cancellationToken)
    {
        var alreadyProcessed = await _readRepository.ExistsByEventIdAsync(request.EventId, cancellationToken);

        if (alreadyProcessed)
        {
            _logger.LogInformation(
                "Skipping duplicate delivery of event {EventId} for donation {DonationId} (already persisted) | CorrelationId={CorrelationId}",
                request.EventId,
                request.DonationId,
                request.CorrelationId);

            return Result.Success();
        }

        var readModel = new DonationReadModel(
            Id: request.DonationId,
            CampaignId: request.CampaignId,
            UserId: request.UserId,
            UserName: request.UserName,
            UserEmail: request.UserEmail,
            Value: request.Value,
            Currency: request.Currency,
            PaymentMethod: request.PaymentMethod,
            DonationDate: request.DonationDate,
            Status: "Confirmed",
            CreatedAtUtc: request.CreatedAt,
            CorrelationId: request.CorrelationId,
            EventId: request.EventId);

        await _readRepository.UpsertAsync(readModel, cancellationToken);

        _logger.LogInformation(
            "Persisted donation {DonationId} to MongoDB read model | EventId={EventId} | CorrelationId={CorrelationId}",
            request.DonationId,
            request.EventId,
            request.CorrelationId);

        return Result.Success();
    }
}
