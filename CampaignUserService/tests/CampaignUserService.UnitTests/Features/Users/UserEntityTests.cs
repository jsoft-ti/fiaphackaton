using CampaignUserService.Domain.Entities;
using CampaignUserService.Domain.Enums;
using CampaignUserService.Domain.Exceptions;
using FluentAssertions;
using Xunit;

namespace CampaignUserService.UnitTests.Features.Users;

public class UserEntityTests
{
    [Fact]
    public void Create_ShouldNormalizeEmailToLowerCase()
    {
        var user = User.Create("Jane", "Doe", "Jane.DOE@Example.COM", "hash", null, null, null);

        user.Email.Should().Be("jane.doe@example.com");
    }

    [Fact]
    public void Create_ShouldStartAsActiveAndEmailNotConfirmed()
    {
        var user = User.Create("Jane", "Doe", "jane.doe@example.com", "hash", null, null, null);

        user.Status.Should().Be(UserStatus.Active);
        user.EmailConfirmed.Should().BeFalse();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void Create_ShouldThrow_WhenFirstNameIsMissing(string firstName)
    {
        var act = () => User.Create(firstName, "Doe", "jane.doe@example.com", "hash", null, null, null);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void AssignRole_ShouldNotDuplicateRole_WhenCalledTwiceForSameRole()
    {
        var user = User.Create("Jane", "Doe", "jane.doe@example.com", "hash", null, null, null);
        var role = Role.Create(RoleName.Doador, "Doador role");

        user.AssignRole(role, DateTime.UtcNow);
        user.AssignRole(role, DateTime.UtcNow);

        user.UserRoles.Should().HaveCount(1);
    }

    [Fact]
    public void ReplaceRole_ShouldSwapExistingRoleForNewOne()
    {
        var user = User.Create("Jane", "Doe", "jane.doe@example.com", "hash", null, null, null);
        var doadorRole = Role.Create(RoleName.Doador, "Doador role");
        var gestorRole = Role.Create(RoleName.GestorOng, "Gestor role");

        user.AssignRole(doadorRole, DateTime.UtcNow);
        user.ReplaceRole(gestorRole, DateTime.UtcNow);

        user.UserRoles.Should().ContainSingle(ur => ur.RoleId == gestorRole.Id);
    }

    [Fact]
    public void Block_ShouldChangeStatusToBlocked()
    {
        var user = User.Create("Jane", "Doe", "jane.doe@example.com", "hash", null, null, null);

        user.Block(DateTime.UtcNow);

        user.Status.Should().Be(UserStatus.Blocked);
        user.CanAuthenticate().Should().BeFalse();
    }

    [Fact]
    public void SoftDelete_ShouldSetIsDeletedAndDeletedAtUtc()
    {
        var user = User.Create("Jane", "Doe", "jane.doe@example.com", "hash", null, null, null);
        var now = DateTime.UtcNow;

        user.SoftDelete(now);

        user.IsDeleted.Should().BeTrue();
        user.DeletedAtUtc.Should().Be(now);
    }

    [Fact]
    public void RecordSuccessfulLogin_ShouldResetAccessFailedCount()
    {
        var user = User.Create("Jane", "Doe", "jane.doe@example.com", "hash", null, null, null);
        user.RecordFailedLoginAttempt(DateTime.UtcNow);
        user.RecordFailedLoginAttempt(DateTime.UtcNow);

        user.RecordSuccessfulLogin(DateTime.UtcNow);

        user.AccessFailedCount.Should().Be(0);
        user.LastLoginAtUtc.Should().NotBeNull();
    }
}
