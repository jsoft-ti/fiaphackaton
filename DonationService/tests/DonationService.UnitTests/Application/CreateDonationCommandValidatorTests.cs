using DonationService.Application.Features.Donations.Commands.CreateDonation;
using DonationService.SharedKernel.Interfaces;
using FluentAssertions;
using FluentValidation.TestHelper;
using Moq;
using Xunit;

namespace DonationService.UnitTests.Application;

public class CreateDonationCommandValidatorTests
{
    private readonly Mock<ICurrentUserService> _currentUserService = new();

    private CreateDonationCommandValidator CreateValidator() => new(_currentUserService.Object);

    private void SetupAuthenticatedUser()
    {
        _currentUserService.SetupGet(c => c.IsAuthenticated).Returns(true);
        _currentUserService.SetupGet(c => c.UserId).Returns(Guid.NewGuid());
        _currentUserService.SetupGet(c => c.Email).Returns("jane@example.com");
    }

    [Fact]
    public void Validate_WithValidCommand_ShouldNotHaveErrors()
    {
        SetupAuthenticatedUser();
        var command = new CreateDonationCommand(Guid.NewGuid(), 50m, "BRL", "Pix");

        var result = CreateValidator().TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WithEmptyCampaignId_ShouldHaveError()
    {
        SetupAuthenticatedUser();
        var command = new CreateDonationCommand(Guid.Empty, 50m, "BRL", "Pix");

        var result = CreateValidator().TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.CampaignId);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    public void Validate_WithNonPositiveValue_ShouldHaveError(decimal value)
    {
        SetupAuthenticatedUser();
        var command = new CreateDonationCommand(Guid.NewGuid(), value, "BRL", "Pix");

        var result = CreateValidator().TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.Value);
    }

    [Fact]
    public void Validate_WithInvalidCurrency_ShouldHaveError()
    {
        SetupAuthenticatedUser();
        var command = new CreateDonationCommand(Guid.NewGuid(), 50m, "XYZ", "Pix");

        var result = CreateValidator().TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.Currency);
    }

    [Fact]
    public void Validate_WithInvalidPaymentMethod_ShouldHaveError()
    {
        SetupAuthenticatedUser();
        var command = new CreateDonationCommand(Guid.NewGuid(), 50m, "BRL", "Crypto");

        var result = CreateValidator().TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.PaymentMethod);
    }

    [Fact]
    public void Validate_WhenUserIsNotAuthenticated_ShouldHaveError()
    {
        _currentUserService.SetupGet(c => c.IsAuthenticated).Returns(false);
        var command = new CreateDonationCommand(Guid.NewGuid(), 50m, "BRL", "Pix");

        var result = CreateValidator().TestValidate(command);

        result.Errors.Should().Contain(e => e.PropertyName == "User");
    }
}
