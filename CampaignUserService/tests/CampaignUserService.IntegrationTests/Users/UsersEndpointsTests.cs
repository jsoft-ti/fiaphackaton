using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using CampaignUserService.IntegrationTests.Common;
using FluentAssertions;
using Xunit;

namespace CampaignUserService.IntegrationTests.Users;

[Collection(IntegrationTestCollection.Name)]
public class UsersEndpointsTests
{
    private readonly HttpClient _client;

    public UsersEndpointsTests(CustomWebApplicationFactory factory)
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

    private async Task<string> RegisterAndGetAccessTokenAsync()
    {
        var request = CreateUniqueRegisterRequest();
        var response = await _client.PostAsJsonAsync("/api/v1/auth/register", request);
        var body = await response.Content.ReadFromJsonAsync<AuthResultDto>();
        return body!.AccessToken;
    }

    private async Task<string> LoginAsAdminAndGetAccessTokenAsync()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new LoginRequestDto(CustomWebApplicationFactory.TestAdminEmail, CustomWebApplicationFactory.TestAdminPassword));

        var body = await response.Content.ReadFromJsonAsync<AuthResultDto>();
        return body!.AccessToken;
    }

    [Fact]
    public async Task GetMe_ShouldReturnUnauthorized_WhenNoTokenIsProvided()
    {
        var response = await _client.GetAsync("/api/v1/users/me");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetMe_ShouldReturnOwnProfile_WhenTokenIsValid()
    {
        var token = await RegisterAndGetAccessTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.GetAsync("/api/v1/users/me");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<UserDto>();
        body!.Role.Should().Be("Doador");

        _client.DefaultRequestHeaders.Authorization = null;
    }

    [Fact]
    public async Task GetUsers_ShouldReturnForbidden_WhenCallerIsDoador()
    {
        var token = await RegisterAndGetAccessTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.GetAsync("/api/v1/users");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        _client.DefaultRequestHeaders.Authorization = null;
    }

    [Fact]
    public async Task GetUsers_ShouldReturnOk_WhenCallerIsGestorOng()
    {
        var token = await LoginAsAdminAndGetAccessTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.GetAsync("/api/v1/users");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        _client.DefaultRequestHeaders.Authorization = null;
    }

    [Fact]
    public async Task UpdateMe_ShouldPersistChanges_WhenRequestIsValid()
    {
        var token = await RegisterAndGetAccessTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.PutAsJsonAsync(
            "/api/v1/users/me",
            new { FirstName = "Updated", LastName = "Name", PhoneNumber = (string?)null, PhotoUrl = (string?)null, BirthDate = (DateOnly?)null });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<UserDto>();
        body!.FirstName.Should().Be("Updated");

        _client.DefaultRequestHeaders.Authorization = null;
    }
}
