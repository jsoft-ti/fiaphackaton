using DonationService.Application.Features.Donations.Commands.PersistDonation;
using DonationService.Contracts.Events.V1;
using DonationService.SharedKernel.Common;
using DonationService.Worker.Consumers;
using MassTransit;
using MassTransit.Testing;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace DonationService.ConsumerTests;

/// <summary>
/// Exercises <see cref="DonationCreatedConsumer"/> through MassTransit's
/// in-memory test harness. <see cref="ISender"/> is mocked (this project
/// deliberately does not reference DonationService.Infrastructure, so no
/// real MediatR handler/MongoDB dependency is available here) - the goal is
/// to verify the consumer correctly maps the event to
/// <see cref="PersistDonationCommand"/> and reacts appropriately to success
/// and failure, not to re-test handler internals (covered in
/// DonationService.UnitTests).
/// </summary>
public sealed class DonationCreatedConsumerTests
{
    private static DonationCreatedEvent CreateEvent() => new(
        EventId: Guid.NewGuid(),
        CorrelationId: Guid.NewGuid(),
        DonationId: Guid.NewGuid(),
        CampaignId: Guid.NewGuid(),
        UserId: Guid.NewGuid(),
        UserName: "Jane Doe",
        UserEmail: "jane@example.com",
        Value: 80m,
        Currency: "BRL",
        PaymentMethod: "Pix",
        DonationDate: DateTime.UtcNow,
        CreatedAt: DateTime.UtcNow);

    [Fact]
    public async Task Consume_WhenPersistSucceeds_ShouldCompleteWithoutFault()
    {
        var senderMock = new Moq.Mock<ISender>();
        senderMock
            .Setup(s => s.Send(Moq.It.IsAny<PersistDonationCommand>(), Moq.It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        await using var provider = new ServiceCollection()
            .AddSingleton(senderMock.Object)
            .AddMassTransitTestHarness(cfg =>
            {
                cfg.AddConsumer<DonationCreatedConsumer>();
            })
            .BuildServiceProvider(true);

        var harness = provider.GetRequiredService<ITestHarness>();
        await harness.Start();

        try
        {
            var @event = CreateEvent();
            await harness.Bus.Publish(@event);

            Assert.True(await harness.Consumed.Any<DonationCreatedEvent>());

            var consumerHarness = harness.GetConsumerHarness<DonationCreatedConsumer>();
            Assert.True(await consumerHarness.Consumed.Any<DonationCreatedEvent>());

            Assert.False(await harness.Published.Any<Fault<DonationCreatedEvent>>());

            senderMock.Verify(
                s => s.Send(
                    Moq.It.Is<PersistDonationCommand>(c => c.DonationId == @event.DonationId && c.EventId == @event.EventId),
                    Moq.It.IsAny<CancellationToken>()),
                Moq.Times.Once);
        }
        finally
        {
            await harness.Stop();
        }
    }

    [Fact]
    public async Task Consume_WhenPersistFails_ShouldFault()
    {
        var senderMock = new Moq.Mock<ISender>();
        senderMock
            .Setup(s => s.Send(Moq.It.IsAny<PersistDonationCommand>(), Moq.It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure(DonationService.SharedKernel.Errors.Error.Failure("mongo_unavailable", "MongoDB write failed.")));

        await using var provider = new ServiceCollection()
            .AddSingleton(senderMock.Object)
            .AddMassTransitTestHarness(cfg =>
            {
                cfg.AddConsumer<DonationCreatedConsumer>();
            })
            .BuildServiceProvider(true);

        var harness = provider.GetRequiredService<ITestHarness>();
        await harness.Start();

        try
        {
            var @event = CreateEvent();
            await harness.Bus.Publish(@event);

            Assert.True(await harness.Published.Any<Fault<DonationCreatedEvent>>());
        }
        finally
        {
            await harness.Stop();
        }
    }
}
