using DonationService.Api.Common;
using DonationService.Api.Extensions;
using DonationService.Application.Features.Donations.Commands.CreateDonation;
using DonationService.Application.Features.Donations.Queries.GetDonationById;
using DonationService.Application.Features.Donations.Queries.GetMyDonations;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace DonationService.Api.Endpoints;

public static class DonationEndpoints
{
    public static RouteGroupBuilder MapDonationEndpoints(this RouteGroupBuilder group)
    {
        var donations = group.MapGroup("/donations").WithTags("Donations");

        donations.MapPost("/", CreateDonationAsync)
            .WithName("CreateDonation")
            .WithSummary("Creates a donation request. Restricted to authenticated users with the Doador role.")
            .RequireAuthorization(JwtAuthenticationExtensions.DoadorOnlyPolicy)
            .Produces<CreateDonationResult>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);

        donations.MapGet("/me", GetMyDonationsAsync)
            .WithName("GetMyDonations")
            .WithSummary("Lists the authenticated user's own donations (read from the MongoDB read model).")
            .RequireAuthorization()
            .Produces<object>(StatusCodes.Status200OK);

        donations.MapGet("/{id:guid}", GetDonationByIdAsync)
            .WithName("GetDonationById")
            .WithSummary("Retrieves a single donation by id.")
            .RequireAuthorization()
            .Produces<object>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);

        return group;
    }

    private static async Task<IResult> CreateDonationAsync(
        [FromBody] CreateDonationRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var command = new CreateDonationCommand(request.CampaignId, request.Value, request.Currency, request.PaymentMethod);

        var result = await sender.Send(command, cancellationToken);

        return result.ToCreatedResult(r => $"/api/v1/donations/{r.DonationId}");
    }

    private static async Task<IResult> GetDonationByIdAsync(
        Guid id,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetDonationByIdQuery(id), cancellationToken);

        return result.ToOkResult();
    }

    private static async Task<IResult> GetMyDonationsAsync(
        ISender sender,
        CancellationToken cancellationToken,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var query = new GetMyDonationsQuery(
            page <= 0 ? 1 : page,
            pageSize <= 0 ? 20 : pageSize);

        var result = await sender.Send(query, cancellationToken);

        return result.ToOkResult();
    }

    public sealed record CreateDonationRequest(Guid CampaignId, decimal Value, string Currency, string PaymentMethod);
}
