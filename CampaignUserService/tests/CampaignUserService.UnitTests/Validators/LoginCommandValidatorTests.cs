using CampaignUserService.Application.Features.Authentication.Commands;
using FluentValidation.TestHelper;
using Xunit;

namespace CampaignUserService.UnitTests.Validators;

public class LoginCommandValidatorTests
{
    private readonly LoginCommandValidator _sut = new();

    [Fact]
    public void Should_NotHaveErrors_WhenCommandIsValid()
    {
        var command = new LoginCommand("jane.doe@example.com", "any-password", "127.0.0.1", "xunit");
        var result = _sut.TestValidate(command);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Should_HaveError_WhenEmailIsEmpty()
    {
        var command = new LoginCommand(string.Empty, "any-password", "127.0.0.1", "xunit");
        var result = _sut.TestValidate(command);
        result.ShouldHaveValidationErrorFor(c => c.Email);
    }

    [Fact]
    public void Should_HaveError_WhenPasswordIsEmpty()
    {
        var command = new LoginCommand("jane.doe@example.com", string.Empty, "127.0.0.1", "xunit");
        var result = _sut.TestValidate(command);
        result.ShouldHaveValidationErrorFor(c => c.Password);
    }
}
