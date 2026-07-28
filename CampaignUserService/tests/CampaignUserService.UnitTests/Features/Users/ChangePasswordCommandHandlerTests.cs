using CampaignUserService.Application.Common.Interfaces;
using CampaignUserService.Application.Features.Users.Commands;
using CampaignUserService.Domain.Entities;
using CampaignUserService.Domain.Repositories;
using CampaignUserService.SharedKernel.Interfaces;
using FluentAssertions;
using Moq;
using Xunit;

namespace CampaignUserService.UnitTests.Features.Users;

public class ChangePasswordCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly Mock<IRefreshTokenRepository> _refreshTokenRepository = new();
    private readonly Mock<IPasswordHasher> _passwordHasher = new();
    private readonly Mock<IAuditService> _auditService = new();
    private readonly Mock<IDateTimeProvider> _dateTimeProvider = new();

    private readonly ChangePasswordCommandHandler _sut;

    public ChangePasswordCommandHandlerTests()
    {
        _unitOfWork.Setup(u => u.Users).Returns(_userRepository.Object);
        _unitOfWork.Setup(u => u.RefreshTokens).Returns(_refreshTokenRepository.Object);
        _dateTimeProvider.Setup(d => d.UtcNow).Returns(DateTime.UtcNow);

        _sut = new ChangePasswordCommandHandler(
            _unitOfWork.Object,
            _passwordHasher.Object,
            _auditService.Object,
            _dateTimeProvider.Object);
    }

    [Fact]
    public async Task Handle_ShouldReturnNotFound_WhenUserDoesNotExist()
    {
        _userRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>(), false))
            .ReturnsAsync((User?)null);

        var result = await _sut.Handle(
            new ChangePasswordCommand(Guid.NewGuid(), "current", "NewP@ss1", "NewP@ss1"),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("user_not_found");
    }

    [Fact]
    public async Task Handle_ShouldReturnValidationError_WhenCurrentPasswordIsIncorrect()
    {
        var user = User.Create("Jane", "Doe", "jane.doe@example.com", "current-hash", null, null, null);

        _userRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>(), false))
            .ReturnsAsync(user);
        _passwordHasher.Setup(p => p.Verify(It.IsAny<string>(), It.IsAny<string>())).Returns(false);

        var result = await _sut.Handle(
            new ChangePasswordCommand(user.Id, "wrong-current", "NewP@ss1", "NewP@ss1"),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("invalid_current_password");
    }

    [Fact]
    public async Task Handle_ShouldChangePasswordAndRevokeSessions_WhenRequestIsValid()
    {
        var user = User.Create("Jane", "Doe", "jane.doe@example.com", "current-hash", null, null, null);

        _userRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>(), false))
            .ReturnsAsync(user);
        _passwordHasher.Setup(p => p.Verify(It.IsAny<string>(), It.IsAny<string>())).Returns(true);
        _passwordHasher.Setup(p => p.Hash(It.IsAny<string>())).Returns("new-hash");

        var result = await _sut.Handle(
            new ChangePasswordCommand(user.Id, "current-password", "NewP@ss1", "NewP@ss1"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        user.PasswordHash.Should().Be("new-hash");
        _refreshTokenRepository.Verify(
            r => r.RevokeAllActiveForUserAsync(user.Id, It.IsAny<DateTime>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
