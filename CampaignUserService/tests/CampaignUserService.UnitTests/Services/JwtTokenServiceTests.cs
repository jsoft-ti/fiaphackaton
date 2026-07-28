using CampaignUserService.Application.Common.Models;
using CampaignUserService.Domain.Entities;
using CampaignUserService.Infrastructure.Security;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Xunit;

namespace CampaignUserService.UnitTests.Services;

public class JwtTokenServiceTests
{
    private readonly JwtTokenService _sut;
    private readonly JwtSettings _settings;

    public JwtTokenServiceTests()
    {
        _settings = new JwtSettings
        {
            Secret = "unit-test-secret-key-with-at-least-32-chars",
            Issuer = "CampaignUserService.Tests",
            Audience = "CampaignUserService.Tests.Clients",
            AccessTokenExpirationMinutes = 15,
            RefreshTokenExpirationDays = 7
        };

        _sut = new JwtTokenService(Options.Create(_settings));
    }

    [Fact]
    public void GenerateAccessToken_ShouldReturnNonEmptyToken_WithExpectedExpiration()
    {
        var user = User.Create("Jane", "Doe", "jane.doe@example.com", "hash", null, null, null);

        var before = DateTime.UtcNow;
        var result = _sut.GenerateAccessToken(user, "Doador");
        var after = DateTime.UtcNow;

        result.Token.Should().NotBeNullOrWhiteSpace();
        result.Jti.Should().NotBeNullOrWhiteSpace();
        result.ExpiresAtUtc.Should().BeOnOrAfter(before.AddMinutes(15)).And.BeOnOrBefore(after.AddMinutes(15).AddSeconds(5));
    }

    [Fact]
    public void GenerateAccessToken_ShouldProduceDifferentJti_ForEachCall()
    {
        var user = User.Create("Jane", "Doe", "jane.doe@example.com", "hash", null, null, null);

        var first = _sut.GenerateAccessToken(user, "Doador");
        var second = _sut.GenerateAccessToken(user, "Doador");

        first.Jti.Should().NotBe(second.Jti);
    }

    [Fact]
    public void GenerateRefreshTokenValue_ShouldReturnUniqueValues()
    {
        var first = _sut.GenerateRefreshTokenValue();
        var second = _sut.GenerateRefreshTokenValue();

        first.Should().NotBe(second);
        first.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void HashRefreshToken_ShouldBeDeterministic_ForSameInput()
    {
        const string rawToken = "some-raw-refresh-token-value";

        var hash1 = _sut.HashRefreshToken(rawToken);
        var hash2 = _sut.HashRefreshToken(rawToken);

        hash1.Should().Be(hash2);
    }

    [Fact]
    public void HashRefreshToken_ShouldNeverEqualRawToken()
    {
        const string rawToken = "some-raw-refresh-token-value";

        var hash = _sut.HashRefreshToken(rawToken);

        hash.Should().NotBe(rawToken);
    }

    [Fact]
    public void AccessTokenLifetime_ShouldMatchConfiguredMinutes()
    {
        _sut.AccessTokenLifetime.Should().Be(TimeSpan.FromMinutes(_settings.AccessTokenExpirationMinutes));
    }

    [Fact]
    public void RefreshTokenLifetime_ShouldMatchConfiguredDays()
    {
        _sut.RefreshTokenLifetime.Should().Be(TimeSpan.FromDays(_settings.RefreshTokenExpirationDays));
    }
}
