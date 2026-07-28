using CampaignUserService.Infrastructure.Security;
using FluentAssertions;
using Xunit;

namespace CampaignUserService.UnitTests.Services;

public class BCryptPasswordHasherTests
{
    private readonly BCryptPasswordHasher _sut = new();

    [Fact]
    public void Hash_ShouldReturnDifferentValue_ThanPlainTextPassword()
    {
        const string password = "MyStrongP@ssw0rd";

        var hash = _sut.Hash(password);

        hash.Should().NotBeNullOrWhiteSpace();
        hash.Should().NotBe(password);
    }

    [Fact]
    public void Hash_ShouldProduceDifferentHashes_ForSamePassword_DueToRandomSalt()
    {
        const string password = "MyStrongP@ssw0rd";

        var hash1 = _sut.Hash(password);
        var hash2 = _sut.Hash(password);

        hash1.Should().NotBe(hash2);
    }

    [Fact]
    public void Verify_ShouldReturnTrue_WhenPasswordMatchesHash()
    {
        const string password = "MyStrongP@ssw0rd";
        var hash = _sut.Hash(password);

        var result = _sut.Verify(password, hash);

        result.Should().BeTrue();
    }

    [Fact]
    public void Verify_ShouldReturnFalse_WhenPasswordDoesNotMatchHash()
    {
        var hash = _sut.Hash("MyStrongP@ssw0rd");

        var result = _sut.Verify("SomeOtherPassword1!", hash);

        result.Should().BeFalse();
    }

    [Fact]
    public void Verify_ShouldReturnFalse_WhenHashIsMalformed()
    {
        var result = _sut.Verify("MyStrongP@ssw0rd", "not-a-valid-bcrypt-hash");

        result.Should().BeFalse();
    }
}
