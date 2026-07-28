using CampaignUserService.Application.Common.Interfaces;
using CampaignUserService.Application.Features.Authentication.Commands;
using CampaignUserService.Domain.Entities;
using CampaignUserService.Domain.Enums;
using CampaignUserService.Domain.Repositories;
using CampaignUserService.SharedKernel.Interfaces;
using FluentAssertions;
using Moq;
using Xunit;

namespace CampaignUserService.UnitTests.Features.Authentication;

public class LoginCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly Mock<IRefreshTokenRepository> _refreshTokenRepository = new();
    private readonly Mock<IPasswordHasher> _passwordHasher = new();
    private readonly Mock<IJwtTokenService> _jwtTokenService = new();
    private readonly Mock<IAuditService> _auditService = new();
    private readonly Mock<IDateTimeProvider> _dateTimeProvider = new();

    private readonly LoginCommandHandler _sut;

    public LoginCommandHandlerTests()
    {
        _unitOfWork.Setup(u => u.Users).Returns(_userRepository.Object);
        _unitOfWork.Setup(u => u.RefreshTokens).Returns(_refreshTokenRepository.Object);
        _dateTimeProvider.Setup(d => d.UtcNow).Returns(new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));

        _sut = new LoginCommandHandler(
            _unitOfWork.Object,
            _passwordHasher.Object,
            _jwtTokenService.Object,
            _auditService.Object,
            _dateTimeProvider.Object);
    }

    private static User CreateUserWithRole(RoleName roleName)
    {
        var user = User.Create("Jane", "Doe", "jane.doe@example.com", "hashed-password", null, null, null);
        var role = Role.Create(roleName, $"{roleName} role");
        user.AssignRole(role, DateTime.UtcNow);
        return user;
    }

    [Fact]
    public async Task Handle_ShouldReturnUnauthorized_WhenUserDoesNotExist()
    {
        _userRepository.Setup(r => r.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>(), true))
            .ReturnsAsync((User?)null);

        var result = await _sut.Handle(
            new LoginCommand("missing@example.com", "any-password", "127.0.0.1", "xunit"),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("invalid_credentials");
    }

    [Fact]
    public async Task Handle_ShouldReturnUnauthorized_WhenPasswordDoesNotMatch()
    {
        var user = CreateUserWithRole(RoleName.Doador);

        _userRepository.Setup(r => r.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>(), true))
            .ReturnsAsync(user);
        _passwordHasher.Setup(p => p.Verify(It.IsAny<string>(), It.IsAny<string>())).Returns(false);

        var result = await _sut.Handle(
            new LoginCommand(user.Email, "wrong-password", "127.0.0.1", "xunit"),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("invalid_credentials");
    }

    [Fact]
    public async Task Handle_ShouldReturnForbidden_WhenUserIsBlocked()
    {
        var user = CreateUserWithRole(RoleName.Doador);
        user.Block(DateTime.UtcNow);

        _userRepository.Setup(r => r.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>(), true))
            .ReturnsAsync(user);
        _passwordHasher.Setup(p => p.Verify(It.IsAny<string>(), It.IsAny<string>())).Returns(true);

        var result = await _sut.Handle(
            new LoginCommand(user.Email, "correct-password", "127.0.0.1", "xunit"),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("user_blocked");
    }

    [Fact]
    public async Task Handle_ShouldReturnAuthResult_WhenCredentialsAreValid()
    {
        var user = CreateUserWithRole(RoleName.GestorOng);

        _userRepository.Setup(r => r.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>(), true))
            .ReturnsAsync(user);
        _passwordHasher.Setup(p => p.Verify(It.IsAny<string>(), It.IsAny<string>())).Returns(true);
        _jwtTokenService.Setup(j => j.GenerateAccessToken(It.IsAny<User>(), "GestorOng"))
            .Returns(new AccessTokenResult("access-token", "jti-123", DateTime.UtcNow.AddMinutes(15)));
        _jwtTokenService.Setup(j => j.GenerateRefreshTokenValue()).Returns("raw-refresh-token");
        _jwtTokenService.Setup(j => j.HashRefreshToken(It.IsAny<string>())).Returns("hashed-refresh-token");
        _jwtTokenService.Setup(j => j.RefreshTokenLifetime).Returns(TimeSpan.FromDays(7));

        var result = await _sut.Handle(
            new LoginCommand(user.Email, "correct-password", "127.0.0.1", "xunit"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Role.Should().Be(nameof(RoleName.GestorOng));
        result.Value.UserId.Should().Be(user.Id);
        _refreshTokenRepository.Verify(r => r.Add(It.IsAny<RefreshToken>()), Times.Once);
    }
}
