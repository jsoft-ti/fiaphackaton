using DonationService.Application.Features.Donations.Commands.PersistDonation;
using DonationService.Domain.ReadModels;
using DonationService.Domain.Repositories;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace DonationService.UnitTests.Application;

public class PersistDonationCommandHandlerTests
{
    private readonly Mock<IDonationReadRepository> _readRepository = new();

    private PersistDonationCommandHandler CreateHandler() =>
        new(_readRepository.Object, Mock.Of<ILogger<PersistDonationCommandHandler>>());

    private static PersistDonationCommand CreateCommand() => new(
        EventId: Guid.NewGuid(),
        CorrelationId: Guid.NewGuid(),
        DonationId: Guid.NewGuid(),
        CampaignId: Guid.NewGuid(),
        UserId: Guid.NewGuid(),
        UserName: "Jane Doe",
        UserEmail: "jane@example.com",
        Value: 75m,
        Currency: "BRL",
        PaymentMethod: "Pix",
        DonationDate: DateTime.UtcNow,
        CreatedAt: DateTime.UtcNow);

    [Fact]
    public async Task Handle_WhenEventNotYetProcessed_ShouldUpsertAndSucceed()
    {
        var command = CreateCommand();

        _readRepository
            .Setup(r => r.ExistsByEventIdAsync(command.EventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var result = await CreateHandler().Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _readRepository.Verify(
            r => r.UpsertAsync(It.Is<DonationReadModel>(m => m.Id == command.DonationId), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WhenEventAlreadyProcessed_ShouldSkipUpsertButStillSucceed()
    {
        var command = CreateCommand();

        _readRepository
            .Setup(r => r.ExistsByEventIdAsync(command.EventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await CreateHandler().Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _readRepository.Verify(
            r => r.UpsertAsync(It.IsAny<DonationReadModel>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
