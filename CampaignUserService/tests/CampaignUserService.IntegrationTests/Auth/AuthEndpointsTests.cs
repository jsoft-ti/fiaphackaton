using System.Net;
using System.Net.Http.Json;
using CampaignUserService.IntegrationTests.Common;
using FluentAssertions;
using Xunit;

namespace CampaignUserService.IntegrationTests.Auth;

[Collection(IntegrationTestCollection.Name)]
public class AuthEndpointsTests
{
    private readonly HttpClient _client;

    public AuthEndpointsTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    private static RegisterRequestDto CreateUniqueRegisterRequest()
    {
        var unique = Guid.NewGuid().ToString("N")[..8];
        return new RegisterRequestDto(
            "Jane",
            "Doe",
            $"jane.doe.{unique}@example.com",
            "StrongP@ss1",
            "StrongP@ss1");
    }

    [Fact]
    public async Task Register_ShouldReturnOkWithTokens_WhenRequestIsValid()
    {
        var request = CreateUniqueRegisterRequest();

        var response = await _client.PostAsJsonAsync("/api/v1/auth/register", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<AuthResultDto>();
        body.Should().NotBeNull();
        body!.AccessToken.Should().NotBeNullOrWhiteSpace();
        body.RefreshToken.Should().NotBeNullOrWhiteSpace();
        body.Role.Should().Be("Doador");
        body.Email.Should().Be(request.Email);
    }

    [Fact]
    public async Task Register_ShouldReturnConflict_WhenEmailAlreadyRegistered()
    {
        var request = CreateUniqueRegisterRequest();
        await _client.PostAsJsonAsync("/api/v1/auth/register", request);

        var response = await _client.PostAsJsonAsync("/api/v1/auth/register", request);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Register_ShouldReturnBadRequest_WhenPasswordIsWeak()
    {
        var request = CreateUniqueRegisterRequest() with { Password = "weak", ConfirmPassword = "weak" };

        var response = await _client.PostAsJsonAsync("/api/v1/auth/register", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Login_ShouldReturnOkWithTokens_WhenCredentialsAreValid()
    {
        var registerRequest = CreateUniqueRegisterRequest();
        await _client.PostAsJsonAsync("/api/v1/auth/register", registerRequest);

        var response = await _client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new LoginRequestDto(registerRequest.Email, registerRequest.Password));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<AuthResultDto>();
        body!.Email.Should().Be(registerRequest.Email);
    }

    [Fact]
    public async Task Login_ShouldReturnUnauthorized_WhenPasswordIsWrong()
    {
        var registerRequest = CreateUniqueRegisterRequest();
        await _client.PostAsJsonAsync("/api/v1/auth/register", registerRequest);

        var response = await _client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new LoginRequestDto(registerRequest.Email, "WrongPassword1!"));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Refresh_ShouldReturnNewTokens_WhenRefreshTokenIsValid()
    {
        var registerRequest = CreateUniqueRegisterRequest();
        var registerResponse = await _client.PostAsJsonAsync("/api/v1/auth/register", registerRequest);
        var registerBody = await registerResponse.Content.ReadFromJsonAsync<AuthResultDto>();

        var response = await _client.PostAsJsonAsync(
            "/api/v1/auth/refresh",
            new { RefreshToken = registerBody!.RefreshToken });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<AuthResultDto>();
        body!.AccessToken.Should().NotBe(registerBody.AccessToken);
        body.RefreshToken.Should().NotBe(registerBody.RefreshToken);
    }

    [Fact]
    public async Task Refresh_ShouldReturnUnauthorized_WhenTokenIsReusedAfterRotation()
    {
        var registerRequest = CreateUniqueRegisterRequest();
        var registerResponse = await _client.PostAsJsonAsync("/api/v1/auth/register", registerRequest);
        var registerBody = await registerResponse.Content.ReadFromJsonAsync<AuthResultDto>();

        // First use rotates the token.
        await _client.PostAsJsonAsync("/api/v1/auth/refresh", new { RefreshToken = registerBody!.RefreshToken });

        // Reusing the old (now revoked) token must fail.
        var reuseResponse = await _client.PostAsJsonAsync(
            "/api/v1/auth/refresh",
            new { RefreshToken = registerBody.RefreshToken });

        reuseResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
