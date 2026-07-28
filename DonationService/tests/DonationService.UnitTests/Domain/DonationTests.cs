using DonationService.Domain.Entities;
using DonationService.Domain.Enums;
using DonationService.Domain.Exceptions;
using FluentAssertions;
using Xunit;

namespace DonationService.UnitTests.Domain;

public class DonationTests
{
    private static Donation CreateValidDonation() => Donation.Create(
        campaignId: Guid.NewGuid(),
        userId: Guid.NewGuid(),
        userName: "Jane Doe",
        userEmail: "jane@example.com",
        value: 100m,
        currency: Currency.BRL,
        paymentMethod: PaymentMethod.Pix,
        donationDate: DateTime.UtcNow,
        correlationId: Guid.NewGuid());

    [Fact]
    public void Create_WithValidData_ShouldSucceedWithPendingPublishStatus()
    {
        var donation = CreateValidDonation();

        donation.Status.Should().Be(DonationStatus.PendingPublish);
        donation.EventId.Should().NotBe(Guid.Empty);
        donation.Id.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void Create_WithEmptyCampaignId_ShouldThrowDomainException()
    {
        var act = () => Donation.Create(
            Guid.Empty, Guid.NewGuid(), "Jane", "jane@example.com", 100m,
            Currency.BRL, PaymentMethod.Pix, DateTime.UtcNow, Guid.NewGuid());

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Create_WithZeroValue_ShouldThrowDomainException()
    {
        var act = () => Donation.Create(
            Guid.NewGuid(), Guid.NewGuid(), "Jane", "jane@example.com", 0m,
            Currency.BRL, PaymentMethod.Pix, DateTime.UtcNow, Guid.NewGuid());

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Create_WithBlankUserName_ShouldThrowDomainException()
    {
        var act = () => Donation.Create(
            Guid.NewGuid(), Guid.NewGuid(), "   ", "jane@example.com", 100m,
            Currency.BRL, PaymentMethod.Pix, DateTime.UtcNow, Guid.NewGuid());

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void MarkPublished_ShouldTransitionStatusAndSetUpdatedAt()
    {
        var donation = CreateValidDonation();
        var now = DateTime.UtcNow;

        donation.MarkPublished(now);

        donation.Status.Should().Be(DonationStatus.Published);
        donation.UpdatedAtUtc.Should().Be(now);
    }

    [Fact]
    public void MarkPublishFailed_ShouldTransitionToPublishFailed()
    {
        var donation = CreateValidDonation();

        donation.MarkPublishFailed(DateTime.UtcNow);

        donation.Status.Should().Be(DonationStatus.PublishFailed);
    }
}
