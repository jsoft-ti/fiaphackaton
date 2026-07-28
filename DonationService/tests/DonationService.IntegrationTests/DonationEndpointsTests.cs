using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using DonationService.Api.Endpoints;
using DonationService.Application.Features.Donations.Commands.CreateDonation;
using FluentAssertions;
using Xunit;

namespace DonationService.IntegrationTests;

/// <summary>
/// Requires Docker to be running locally (or a CI runner with Docker
/// available) - Testcontainers boots real Postgres/MongoDB/RabbitMQ
/// instances for each test run via <see cref="DonationServiceApiFactory"/>.
/// </summary>
public sealed class DonationEndpointsTests : IClassFixture<DonationServiceApiFactory>
{
    private readonly DonationServiceApiFactory _factory;

    public DonationEndpointsTests(DonationServiceApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task CreateDonation_WithoutToken_ShouldReturnUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/donations", new DonationEndpoints.CreateDonationRequest(
            Guid.NewGuid(), 50m, "BRL", "Pix"));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateDonation_WithGestorOngRole_ShouldReturnForbidden()
    {
        var client = _factory.CreateClient();
        var token = TestJwtTokenFactory.CreateToken(Guid.NewGuid(), "gestor@example.com", "Gestor", "GestorOng");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.PostAsJsonAsync("/api/v1/donations", new DonationEndpoints.CreateDonationRequest(
            Guid.NewGuid(), 50m, "BRL", "Pix"));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task CreateDonation_WithDoadorRoleAndValidCampaign_ShouldReturnCreated()
    {
        var client = _factory.CreateClient();
        var token = TestJwtTokenFactory.CreateToken(Guid.NewGuid(), "doador@example.com", "Doador Teste", "Doador");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        _factory.CampaignServiceClientStub.Result = new(true, true, true, "Campanha de Teste");

        var response = await client.PostAsJsonAsync("/api/v1/donations", new DonationEndpoints.CreateDonationRequest(
            Guid.NewGuid(), 50m, "BRL", "Pix"));

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var result = await response.Content.ReadFromJsonAsync<CreateDonationResult>();
        result.Should().NotBeNull();
        result!.Status.Should().Be("Published");
    }

    [Fact]
    public async Task CreateDonation_WhenCampaignDoesNotExist_ShouldReturnNotFound()
    {
        var client = _factory.CreateClient();
        var token = TestJwtTokenFactory.CreateToken(Guid.NewGuid(), "doador@example.com", "Doador Teste", "Doador");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        _factory.CampaignServiceClientStub.Result = DonationService.Application.Common.Interfaces.CampaignValidationResult.NotFound();

        var response = await client.PostAsJsonAsync("/api/v1/donations", new DonationEndpoints.CreateDonationRequest(
            Guid.NewGuid(), 50m, "BRL", "Pix"));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetMyDonations_WithoutToken_ShouldReturnUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/v1/donations/me");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
