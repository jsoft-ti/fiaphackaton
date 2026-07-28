using CampaignUserService.Application.Features.Authentication.Commands;
using FluentAssertions;
using FluentValidation.TestHelper;
using Xunit;

namespace CampaignUserService.UnitTests.Validators;

public class RegisterCommandValidatorTests
{
    private readonly RegisterCommandValidator _sut = new();

    private static RegisterCommand CreateValidCommand() => new(
        "Jane",
        "Doe",
        "jane.doe@example.com",
        "StrongP@ss1",
        "StrongP@ss1",
        PhoneNumber: "+55 11 91234-5678",
        Cpf: "12345678901",
        BirthDate: new DateOnly(1990, 1, 1),
        IpAddress: "127.0.0.1",
        UserAgent: "xunit");

    [Fact]
    public void Should_NotHaveErrors_WhenCommandIsValid()
    {
        var result = _sut.TestValidate(CreateValidCommand());
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-an-email")]
    public void Should_HaveError_WhenEmailIsInvalid(string email)
    {
        var command = CreateValidCommand() with { Email = email };
        var result = _sut.TestValidate(command);
        result.ShouldHaveValidationErrorFor(c => c.Email);
    }

    [Theory]
    [InlineData("short1!")]
    [InlineData("nouppercase1!")]
    [InlineData("NOLOWERCASE1!")]
    [InlineData("NoDigitsHere!")]
    [InlineData("NoSpecialChar1")]
    public void Should_HaveError_WhenPasswordDoesNotMeetComplexityRules(string password)
    {
        var command = CreateValidCommand() with { Password = password, ConfirmPassword = password };
        var result = _sut.TestValidate(command);
        result.ShouldHaveValidationErrorFor(c => c.Password);
    }

    [Fact]
    public void Should_HaveError_WhenConfirmPasswordDoesNotMatch()
    {
        var command = CreateValidCommand() with { ConfirmPassword = "DifferentP@ss1" };
        var result = _sut.TestValidate(command);
        result.ShouldHaveValidationErrorFor(c => c.ConfirmPassword);
    }

    [Fact]
    public void Should_HaveError_WhenCpfHasInvalidLength()
    {
        var command = CreateValidCommand() with { Cpf = "123" };
        var result = _sut.TestValidate(command);
        result.ShouldHaveValidationErrorFor(c => c.Cpf);
    }

    [Fact]
    public void Should_NotHaveError_WhenOptionalFieldsAreNull()
    {
        var command = CreateValidCommand() with { PhoneNumber = null, Cpf = null, BirthDate = null };
        var result = _sut.TestValidate(command);
        result.ShouldNotHaveAnyValidationErrors();
    }
}
