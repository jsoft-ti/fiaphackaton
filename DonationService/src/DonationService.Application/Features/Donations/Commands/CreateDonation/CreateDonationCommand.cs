using System.Text.Json;
using DonationService.Application.Common.Interfaces;
using DonationService.Contracts.Events.V1;
using DonationService.Domain.Entities;
using DonationService.Domain.Enums;
using DonationService.Domain.Repositories;
using DonationService.SharedKernel.Common;
using DonationService.SharedKernel.Errors;
using DonationService.SharedKernel.Interfaces;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;

namespace DonationService.Application.Features.Donations.Commands.CreateDonation;

/// <summary>
/// Creates a donation request. Only reachable by callers holding the
/// "Doador" role - enforced upstream by the API's authorization policy, not
/// by this handler (RBAC is policy-based only, per architectural rule).
/// </summary>
public sealed record CreateDonationCommand(
    Guid CampaignId,
    decimal Value,
    string Currency,
    string PaymentMethod) : IRequest<Result<CreateDonationResult>>;

public sealed record CreateDonationResult(
    Guid DonationId,
    Guid CampaignId,
    decimal Value,
    string Currency,
    string PaymentMethod,
    DateTime DonationDate,
    string Status);

public sealed class CreateDonationCommandValidator : AbstractValidator<CreateDonationCommand>
{
    public CreateDonationCommandValidator(ICurrentUserService currentUserService)
    {
        RuleFor(x => x.CampaignId)
            .NotEmpty()
            .WithMessage("CampaignId is required.");

        RuleFor(x => x.Value)
            .GreaterThan(0m)
            .WithMessage("Value must be greater than zero.");

        RuleFor(x => x.Currency)
            .NotEmpty()
            .Must(code => Enum.TryParse<Domain.Enums.Currency>(code, ignoreCase: true, out _))
            .WithMessage("Currency must be one of: BRL, USD, EUR.");

        RuleFor(x => x.PaymentMethod)
            .NotEmpty()
            .Must(method => Enum.TryParse<Domain.Enums.PaymentMethod>(method, ignoreCase: true, out _))
            .WithMessage("PaymentMethod must be one of: CreditCard, DebitCard, Pix, BankTransfer, Boleto.");

        // "usuário autenticado" is itself a mandated FluentValidation rule, not
        // just an [Authorize] concern - fail fast if the JWT somehow produced
        // no usable identity before any domain/infrastructure work happens.
        RuleFor(x => x)
            .Must(_ => currentUserService.IsAuthenticated
                       && currentUserService.UserId.HasValue
                       && currentUserService.UserId.Value != Guid.Empty
                       && !string.IsNullOrWhiteSpace(currentUserService.Email))
            .WithMessage("An authenticated user with a valid identity is required.")
            .WithName("User");
    }
}

public sealed class CreateDonationCommandHandler
    : IRequestHandler<CreateDonationCommand, Result<CreateDonationResult>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICampaignServiceClient _campaignServiceClient;
    private readonly IEventPublisher _eventPublisher;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ILogger<CreateDonationCommandHandler> _logger;

    public CreateDonationCommandHandler(
        IUnitOfWork unitOfWork,
        ICampaignServiceClient campaignServiceClient,
        IEventPublisher eventPublisher,
        ICurrentUserService currentUserService,
        IDateTimeProvider dateTimeProvider,
        ILogger<CreateDonationCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _campaignServiceClient = campaignServiceClient;
        _eventPublisher = eventPublisher;
        _currentUserService = currentUserService;
        _dateTimeProvider = dateTimeProvider;
        _logger = logger;
    }

    public async Task<Result<CreateDonationResult>> Handle(
        CreateDonationCommand request,
        CancellationToken cancellationToken)
    {
        var campaign = await _campaignServiceClient.ValidateCampaignAsync(request.CampaignId, cancellationToken);

        if (!campaign.Exists)
        {
            return Result.Failure<CreateDonationResult>(
                Error.NotFound("campaign_not_found", $"Campaign '{request.CampaignId}' was not found."));
        }

        if (!campaign.IsActive)
        {
            return Result.Failure<CreateDonationResult>(
                Error.Validation("campaign_inactive", "The campaign is not currently active."));
        }

        if (!campaign.AcceptsDonations)
        {
            return Result.Failure<CreateDonationResult>(
                Error.Validation("campaign_not_accepting_donations", "The campaign is not accepting donations."));
        }

        var currency = Enum.Parse<Domain.Enums.Currency>(request.Currency, ignoreCase: true);
        var paymentMethod = Enum.Parse<Domain.Enums.PaymentMethod>(request.PaymentMethod, ignoreCase: true);
        var utcNow = _dateTimeProvider.UtcNow;

        var donation = Donation.Create(
            campaignId: request.CampaignId,
            userId: _currentUserService.UserId!.Value,
            userName: _currentUserService.Name ?? _currentUserService.Email!,
            userEmail: _currentUserService.Email!,
            value: request.Value,
            currency: currency,
            paymentMethod: paymentMethod,
            donationDate: utcNow,
            correlationId: Guid.TryParse(_currentUserService.CorrelationId, out var parsedCorrelationId)
                ? parsedCorrelationId
                : Guid.NewGuid());

        _unitOfWork.Donations.Add(donation);

        var history = DonationHistory.Create(
            donation.Id,
            DonationStatus.PendingPublish,
            DonationStatus.PendingPublish,
            "Donation request received and validated against CampaignService.");

        _unitOfWork.DonationHistories.Add(history);

        var integrationEvent = new DonationCreatedEvent(
            EventId: donation.EventId,
            CorrelationId: donation.CorrelationId,
            DonationId: donation.Id,
            CampaignId: donation.CampaignId,
            UserId: donation.UserId,
            UserName: donation.UserName,
            UserEmail: donation.UserEmail,
            Value: donation.Value,
            Currency: donation.Currency.ToString(),
            PaymentMethod: donation.PaymentMethod.ToString(),
            DonationDate: donation.DonationDate,
            CreatedAt: utcNow);

        // Publishing here (before SaveChangesAsync) lets MassTransit's EF Core
        // transactional outbox capture the message in the same change tracker
        // as the Donation/DonationHistory/DonationEvent rows below. Nothing is
        // sent to RabbitMQ until the single SaveChangesAsync transaction
        // commits successfully - guaranteeing no event is ever published for a
        // donation that failed to persist, and no donation is ever persisted
        // without its event eventually reaching the broker.
        await _eventPublisher.PublishAsync(integrationEvent, cancellationToken);

        var donationEvent = DonationEvent.Create(
            donation.Id,
            integrationEvent.EventId,
            integrationEvent.CorrelationId,
            typeof(DonationCreatedEvent).FullName ?? nameof(DonationCreatedEvent),
            JsonSerializer.Serialize(integrationEvent));

        _unitOfWork.DonationEvents.Add(donationEvent);

        donation.MarkPublished(utcNow);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Donation {DonationId} created for campaign {CampaignId} by user {UserId} | EventId={EventId} | CorrelationId={CorrelationId}",
            donation.Id,
            donation.CampaignId,
            donation.UserId,
            donation.EventId,
            donation.CorrelationId);

        return Result.Success(new CreateDonationResult(
            donation.Id,
            donation.CampaignId,
            donation.Value,
            donation.Currency.ToString(),
            donation.PaymentMethod.ToString(),
            donation.DonationDate,
            donation.Status.ToString()));
    }
}
