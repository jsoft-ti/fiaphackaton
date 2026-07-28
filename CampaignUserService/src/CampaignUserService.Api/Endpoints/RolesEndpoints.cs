using Asp.Versioning;
using Asp.Versioning.Builder;
using CampaignUserService.Api.Authorization;
using CampaignUserService.Api.Contracts;
using CampaignUserService.Api.Extensions;
using CampaignUserService.Application.Features.Roles.Commands;
using CampaignUserService.Application.Features.Roles.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace CampaignUserService.Api.Endpoints;

public static class RolesEndpoints
{
    public static IEndpointRouteBuilder MapRolesEndpoints(this IEndpointRouteBuilder app, ApiVersionSet versionSet)
    {
        var group = app.MapGroup("/api/v{version:apiVersion}/roles")
            .WithApiVersionSet(versionSet)
            .MapToApiVersion(new ApiVersion(1, 0))
            .WithTags("Roles")
            .RequireRateLimiting(RateLimitingExtensions.GlobalPolicy)
            .RequireAuthorization(PolicyNames.GestorOngOnly);

        group.MapGet("/", GetRolesAsync)
            .WithName("GetRoles")
            .WithSummary("Lista as roles do sistema (Doador, GestorOng).")
            .Produces(StatusCodes.Status200OK);

        group.MapPost("/", CreateRoleAsync)
            .WithName("CreateRole")
            .WithSummary("Cadastra/atualiza a descrição de uma role.")
            .Produces(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status409Conflict);

        return app;
    }

    private static async Task<IResult> GetRolesAsync(ISender sender, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetRolesQuery(), cancellationToken);
        return result.ToOkOrProblem();
    }

    private static async Task<IResult> CreateRoleAsync(
        [FromBody] CreateRoleRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new CreateRoleCommand(request.Name, request.Description), cancellationToken);
        return result.IsSuccess
            ? Results.Created($"/api/v1/roles/{result.Value.Id}", result.Value)
            : result.ToOkOrProblem();
    }
}
