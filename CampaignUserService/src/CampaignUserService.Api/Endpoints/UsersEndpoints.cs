using Asp.Versioning;
using Asp.Versioning.Builder;
using CampaignUserService.Api.Authorization;
using CampaignUserService.Api.Contracts;
using CampaignUserService.Api.Extensions;
using CampaignUserService.Application.Features.Users.Commands;
using CampaignUserService.Application.Features.Users.Queries;
using CampaignUserService.Domain.Enums;
using CampaignUserService.SharedKernel.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace CampaignUserService.Api.Endpoints;

public static class UsersEndpoints
{
    public static IEndpointRouteBuilder MapUsersEndpoints(this IEndpointRouteBuilder app, ApiVersionSet versionSet)
    {
        var group = app.MapGroup("/api/v{version:apiVersion}/users")
            .WithApiVersionSet(versionSet)
            .MapToApiVersion(new ApiVersion(1, 0))
            .WithTags("Users")
            .RequireRateLimiting(RateLimitingExtensions.GlobalPolicy)
            .RequireAuthorization(PolicyNames.AuthenticatedUser);

        // ----- Self-service (Doador or GestorOng) -----

        group.MapGet("/me", GetMeAsync)
            .WithName("GetMe")
            .WithSummary("Retorna os dados do usuário autenticado.")
            .Produces(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPut("/me", UpdateMeAsync)
            .WithName("UpdateMe")
            .WithSummary("Atualiza o perfil do usuário autenticado.")
            .Produces(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest);

        group.MapPut("/me/password", ChangeMyPasswordAsync)
            .WithName("ChangeMyPassword")
            .WithSummary("Altera a senha do usuário autenticado.")
            .Produces(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest);

        group.MapDelete("/me", DeleteMeAsync)
            .WithName("DeleteMe")
            .WithSummary("Exclui (soft delete) a própria conta.")
            .Produces(StatusCodes.Status200OK);

        // ----- Administrative (GestorOng only) -----

        var adminGroup = group.MapGroup("").RequireAuthorization(PolicyNames.GestorOngOnly);

        adminGroup.MapGet("/", GetUsersAsync)
            .WithName("GetUsers")
            .WithSummary("Lista usuários com busca, filtro e paginação.")
            .Produces(StatusCodes.Status200OK);

        adminGroup.MapGet("/{id:guid}", GetUserByIdAsync)
            .WithName("GetUserById")
            .WithSummary("Retorna um usuário pelo id.")
            .Produces(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);

        adminGroup.MapPost("/", CreateUserAsync)
            .WithName("CreateUser")
            .WithSummary("Cria um novo usuário (Doador ou GestorOng).")
            .Produces(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status409Conflict);

        adminGroup.MapPut("/{id:guid}", UpdateUserAsync)
            .WithName("UpdateUser")
            .WithSummary("Atualiza o perfil de qualquer usuário.")
            .Produces(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);

        adminGroup.MapDelete("/{id:guid}", DeleteUserAsync)
            .WithName("DeleteUser")
            .WithSummary("Exclui (soft delete) qualquer usuário.")
            .Produces(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);

        adminGroup.MapPatch("/{id:guid}/activate", ActivateUserAsync)
            .WithName("ActivateUser")
            .WithSummary("Ativa um usuário.")
            .Produces(StatusCodes.Status200OK);

        adminGroup.MapPatch("/{id:guid}/deactivate", DeactivateUserAsync)
            .WithName("DeactivateUser")
            .WithSummary("Desativa um usuário.")
            .Produces(StatusCodes.Status200OK);

        adminGroup.MapPatch("/{id:guid}/block", BlockUserAsync)
            .WithName("BlockUser")
            .WithSummary("Bloqueia um usuário.")
            .Produces(StatusCodes.Status200OK);

        adminGroup.MapPatch("/{id:guid}/roles", ChangeUserRoleAsync)
            .WithName("ChangeUserRole")
            .WithSummary("Altera a role de um usuário.")
            .Produces(StatusCodes.Status200OK);

        adminGroup.MapPost("/{id:guid}/reset-password", AdminResetPasswordAsync)
            .WithName("AdminResetPassword")
            .WithSummary("Força o envio de um link de redefinição de senha para o usuário.")
            .Produces(StatusCodes.Status200OK);

        return app;
    }

    private static async Task<IResult> GetMeAsync(ISender sender, ICurrentUserService currentUser, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetMeQuery(currentUser.UserId!.Value), cancellationToken);
        return result.ToOkOrProblem();
    }

    private static async Task<IResult> UpdateMeAsync(
        [FromBody] UpdateProfileRequest request,
        ISender sender,
        ICurrentUserService currentUser,
        CancellationToken cancellationToken)
    {
        var command = new UpdateProfileCommand(
            currentUser.UserId!.Value,
            request.FirstName,
            request.LastName,
            request.PhoneNumber,
            request.PhotoUrl,
            request.BirthDate);

        var result = await sender.Send(command, cancellationToken);
        return result.ToOkOrProblem();
    }

    private static async Task<IResult> ChangeMyPasswordAsync(
        [FromBody] ChangePasswordRequest request,
        ISender sender,
        ICurrentUserService currentUser,
        CancellationToken cancellationToken)
    {
        var command = new ChangePasswordCommand(
            currentUser.UserId!.Value,
            request.CurrentPassword,
            request.NewPassword,
            request.ConfirmNewPassword);

        var result = await sender.Send(command, cancellationToken);
        return result.ToOkOrProblem();
    }

    private static async Task<IResult> DeleteMeAsync(ISender sender, ICurrentUserService currentUser, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new DeleteMeCommand(currentUser.UserId!.Value), cancellationToken);
        return result.ToOkOrProblem();
    }

    private static async Task<IResult> GetUsersAsync(
        ISender sender,
        CancellationToken cancellationToken,
        [FromQuery] string? search = null,
        [FromQuery] RoleName? role = null,
        [FromQuery] UserStatus? status = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await sender.Send(new GetUsersQuery(search, role, status, page, pageSize), cancellationToken);
        return result.ToOkOrProblem();
    }

    private static async Task<IResult> GetUserByIdAsync(Guid id, ISender sender, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetUserByIdQuery(id), cancellationToken);
        return result.ToOkOrProblem();
    }

    private static async Task<IResult> CreateUserAsync(
        [FromBody] CreateUserRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var command = new CreateUserCommand(
            request.FirstName,
            request.LastName,
            request.Email,
            request.Password,
            request.PhoneNumber,
            request.Cpf,
            request.BirthDate,
            request.Role);

        var result = await sender.Send(command, cancellationToken);
        return result.IsSuccess
            ? Results.Created($"/api/v1/users/{result.Value.Id}", result.Value)
            : result.ToOkOrProblem();
    }

    private static async Task<IResult> UpdateUserAsync(
        Guid id,
        [FromBody] UpdateUserRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var command = new UpdateUserCommand(id, request.FirstName, request.LastName, request.PhoneNumber, request.PhotoUrl, request.BirthDate);
        var result = await sender.Send(command, cancellationToken);
        return result.ToOkOrProblem();
    }

    private static async Task<IResult> DeleteUserAsync(
        Guid id,
        ISender sender,
        ICurrentUserService currentUser,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new DeleteUserCommand(id, currentUser.UserId!.Value), cancellationToken);
        return result.ToOkOrProblem();
    }

    private static async Task<IResult> ActivateUserAsync(
        Guid id,
        ISender sender,
        ICurrentUserService currentUser,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new ActivateUserCommand(id, currentUser.UserId!.Value), cancellationToken);
        return result.ToOkOrProblem();
    }

    private static async Task<IResult> DeactivateUserAsync(
        Guid id,
        ISender sender,
        ICurrentUserService currentUser,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new DeactivateUserCommand(id, currentUser.UserId!.Value), cancellationToken);
        return result.ToOkOrProblem();
    }

    private static async Task<IResult> BlockUserAsync(
        Guid id,
        ISender sender,
        ICurrentUserService currentUser,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new BlockUserCommand(id, currentUser.UserId!.Value), cancellationToken);
        return result.ToOkOrProblem();
    }

    private static async Task<IResult> ChangeUserRoleAsync(
        Guid id,
        [FromBody] ChangeUserRoleRequest request,
        ISender sender,
        ICurrentUserService currentUser,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new ChangeUserRoleCommand(id, request.Role, currentUser.UserId!.Value), cancellationToken);
        return result.ToOkOrProblem();
    }

    private static async Task<IResult> AdminResetPasswordAsync(
        Guid id,
        ISender sender,
        ICurrentUserService currentUser,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new AdminResetPasswordCommand(id, currentUser.UserId!.Value), cancellationToken);
        return result.ToOkOrProblem();
    }
}
