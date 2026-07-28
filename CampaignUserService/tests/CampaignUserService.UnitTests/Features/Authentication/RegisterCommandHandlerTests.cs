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

public class RegisterCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly Mock<IRoleRepository> _roleRepository = new();
    private readonly Mock<IRefreshTokenRepository> _refreshTokenRepository = new();
    private readonly Mock<IPasswordHasher> _passwordHasher = new();
    private readonly Mock<IJwtTokenService> _jwtTokenService = new();
    private readonly Mock<IAuditService> _auditService = new();
    private readonly Mock<IEmailSender> _emailSender = new();
    private readonly Mock<IDateTimeProvider> _dateTimeProvider = new();

    private readonly RegisterCommandHandler _sut;

    public RegisterCommandHandlerTests()
    {
        _unitOfWork.Setup(u => u.Users).Returns(_userRepository.Object);
        _unitOfWork.Setup(u => u.Roles).Returns(_roleRepository.Object);
        _unitOfWork.Setup(u => u.RefreshTokens).Returns(_refreshTokenRepository.Object);
        _dateTimeProvider.Setup(d => d.UtcNow).Returns(new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));

        _sut = new RegisterCommandHandler(
            _unitOfWork.Object,
            _passwordHasher.Object,
            _jwtTokenService.Object,
            _auditService.Object,
            _emailSender.Object,
            _dateTimeProvider.Object);
    }

    private static RegisterCommand CreateValidCommand() => new(
        "Jane",
        "Doe",
        "jane.doe@example.com",
        "StrongP@ss1",
        "StrongP@ss1",
        PhoneNumber: null,
        Cpf: null,
        BirthDate: null,
        IpAddress: "127.0.0.1",
        UserAgent: "xunit-tests");

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenEmailAlreadyExists()
    {
        _userRepository.Setup(r => r.ExistsByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await _sut.Handle(CreateValidCommand(), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("email_already_used");
        _userRepository.Verify(r => r.Add(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldCreateUserWithDoadorRole_WhenRequestIsValid()
    {
        _userRepository.Setup(r => r.ExistsByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var doadorRole = Role.Create(RoleName.Doador, "Doador role");
        _roleRepository.Setup(r => r.GetByNameAsync(RoleName.Doador, It.IsAny<CancellationToken>()))
            .ReturnsAsync(doadorRole);

        _passwordHasher.Setup(p => p.Hash(It.IsAny<string>())).Returns("hashed-password");
        _jwtTokenService.Setup(j => j.GenerateAccessToken(It.IsAny<User>(), "Doador"))
            .Returns(new AccessTokenResult("access-token", "jti-123", DateTime.UtcNow.AddMinutes(15)));
        _jwtTokenService.Setup(j => j.GenerateRefreshTokenValue()).Returns("raw-refresh-token");
        _jwtTokenService.Setup(j => j.HashRefreshToken(It.IsAny<string>())).Returns("hashed-refresh-token");
        _jwtTokenService.Setup(j => j.RefreshTokenLifetime).Returns(TimeSpan.FromDays(7));

        var result = await _sut.Handle(CreateValidCommand(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Role.Should().Be(nameof(RoleName.Doador));
        result.Value.AccessToken.Should().Be("access-token");
        result.Value.RefreshToken.Should().Be("raw-refresh-token");

        _userRepository.Verify(r => r.Add(It.IsAny<User>()), Times.Once);
        _refreshTokenRepository.Verify(r => r.Add(It.IsAny<RefreshToken>()), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
