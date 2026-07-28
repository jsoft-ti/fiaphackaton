using DonationService.Application.Common.Interfaces;
using DonationService.Application.Features.Donations.Commands.CreateDonation;
using DonationService.Contracts.Events.V1;
using DonationService.Domain.Entities;
using DonationService.Domain.Repositories;
using DonationService.SharedKernel.Errors;
using DonationService.SharedKernel.Interfaces;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace DonationService.UnitTests.Application;

public class CreateDonationCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IDonationRepository> _donationRepository = new();
    private readonly Mock<IDonationHistoryRepository> _donationHistoryRepository = new();
    private readonly Mock<IDonationEventRepository> _donationEventRepository = new();
    private readonly Mock<ICampaignServiceClient> _campaignServiceClient = new();
    private readonly Mock<IEventPublisher> _eventPublisher = new();
    private readonly Mock<ICurrentUserService> _currentUserService = new();
    private readonly Mock<IDateTimeProvider> _dateTimeProvider = new();

    private readonly CreateDonationCommand _command = new(
        Guid.NewGuid(), 150m, "BRL", "Pix");

    public CreateDonationCommandHandlerTests()
    {
        _unitOfWork.SetupGet(u => u.Donations).Returns(_donationRepository.Object);
        _unitOfWork.SetupGet(u => u.DonationHistories).Returns(_donationHistoryRepository.Object);
        _unitOfWork.SetupGet(u => u.DonationEvents).Returns(_donationEventRepository.Object);
        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        _currentUserService.SetupGet(c => c.UserId).Returns(Guid.NewGuid());
        _currentUserService.SetupGet(c => c.Email).Returns("jane@example.com");
        _currentUserService.SetupGet(c => c.Name).Returns("Jane Doe");
        _currentUserService.SetupGet(c => c.CorrelationId).Returns(Guid.NewGuid().ToString());

        _dateTimeProvider.SetupGet(d => d.UtcNow).Returns(DateTime.UtcNow);
    }

    private CreateDonationCommandHandler CreateHandler() => new(
        _unitOfWork.Object,
        _campaignServiceClient.Object,
        _eventPublisher.Object,
        _currentUserService.Object,
        _dateTimeProvider.Object,
        Mock.Of<ILogger<CreateDonationCommandHandler>>());

    [Fact]
    public async Task Handle_WhenCampaignDoesNotExist_ShouldReturnNotFoundFailure()
    {
        _campaignServiceClient
            .Setup(c => c.ValidateCampaignAsync(_command.CampaignId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CampaignValidationResult.NotFound());

        var result = await CreateHandler().Handle(_command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.NotFound);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenCampaignIsInactive_ShouldReturnValidationFailure()
    {
        _campaignServiceClient
            .Setup(c => c.ValidateCampaignAsync(_command.CampaignId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CampaignValidationResult(true, false, true, "Campaign"));

        var result = await CreateHandler().Handle(_command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("campaign_inactive");
    }

    [Fact]
    public async Task Handle_WhenCampaignDoesNotAcceptDonations_ShouldReturnValidationFailure()
    {
        _campaignServiceClient
            .Setup(c => c.ValidateCampaignAsync(_command.CampaignId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CampaignValidationResult(true, true, false, "Campaign"));

        var result = await CreateHandler().Handle(_command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("campaign_not_accepting_donations");
    }

    [Fact]
    public async Task Handle_WhenCampaignIsValid_ShouldPersistPublishAndReturnSuccess()
    {
        _campaignServiceClient
            .Setup(c => c.ValidateCampaignAsync(_command.CampaignId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CampaignValidationResult(true, true, true, "Campaign"));

        var result = await CreateHandler().Handle(_command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.CampaignId.Should().Be(_command.CampaignId);
        result.Value.Status.Should().Be("Published");

        _donationRepository.Verify(r => r.Add(It.IsAny<Donation>()), Times.Once);
        _donationHistoryRepository.Verify(r => r.Add(It.IsAny<DonationHistory>()), Times.Once);
        _donationEventRepository.Verify(r => r.Add(It.IsAny<DonationEvent>()), Times.Once);
        _eventPublisher.Verify(
            p => p.PublishAsync(It.IsAny<DonationCreatedEvent>(), It.IsAny<CancellationToken>()),
            Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
