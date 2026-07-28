using Asp.Versioning;
using Asp.Versioning.Builder;
using CampaignUserService.Api.Authorization;
using CampaignUserService.Api.Contracts;
using CampaignUserService.Api.Extensions;
using CampaignUserService.Application.Features.Authentication.Commands;
using CampaignUserService.SharedKernel.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace CampaignUserService.Api.Endpoints;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app, ApiVersionSet versionSet)
    {
        var group = app.MapGroup("/api/v{version:apiVersion}/auth")
            .WithApiVersionSet(versionSet)
            .MapToApiVersion(new ApiVersion(1, 0))
            .WithTags("Authentication")
            .RequireRateLimiting(RateLimitingExtensions.AuthPolicy);

        group.MapPost("/register", RegisterAsync)
            .WithName("Register")
            .WithSummary("Cria uma nova conta de Doador.")
            .Produces(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .AllowAnonymous();

        group.MapPost("/login", LoginAsync)
            .WithName("Login")
            .WithSummary("Autentica um usuário e retorna access/refresh tokens.")
            .Produces(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .AllowAnonymous();

        group.MapPost("/refresh", RefreshAsync)
            .WithName("RefreshToken")
            .WithSummary("Renova o access token a partir de um refresh token válido.")
            .Produces(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .AllowAnonymous();

        group.MapPost("/logout", LogoutAsync)
            .WithName("Logout")
            .WithSummary("Revoga o refresh token informado, encerrando a sessão.")
            .Produces(StatusCodes.Status200OK)
            .RequireAuthorization(PolicyNames.AuthenticatedUser);

        group.MapPost("/forgot-password", ForgotPasswordAsync)
            .WithName("ForgotPassword")
            .WithSummary("Inicia o fluxo de recuperação de senha.")
            .Produces(StatusCodes.Status200OK)
            .AllowAnonymous();

        group.MapPost("/reset-password", ResetPasswordAsync)
            .WithName("ResetPassword")
            .WithSummary("Redefine a senha a partir de um token de recuperação válido.")
            .Produces(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .AllowAnonymous();

        return app;
    }

    private static async Task<IResult> RegisterAsync(
        [FromBody] RegisterRequest request,
        ISender sender,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var command = new RegisterCommand(
            request.FirstName,
            request.LastName,
            request.Email,
            request.Password,
            request.ConfirmPassword,
            request.PhoneNumber,
            request.Cpf,
            request.BirthDate,
            GetClientIp(httpContext),
            GetUserAgent(httpContext));

        var result = await sender.Send(command, cancellationToken);
        return result.ToOkOrProblem();
    }

    private static async Task<IResult> LoginAsync(
        [FromBody] LoginRequest request,
        ISender sender,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var command = new LoginCommand(
            request.Email,
            request.Password,
            GetClientIp(httpContext),
            GetUserAgent(httpContext));

        var result = await sender.Send(command, cancellationToken);
        return result.ToOkOrProblem();
    }

    private static async Task<IResult> RefreshAsync(
        [FromBody] RefreshTokenRequest request,
        ISender sender,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var command = new RefreshTokenCommand(
            request.RefreshToken,
            GetClientIp(httpContext),
            GetUserAgent(httpContext));

        var result = await sender.Send(command, cancellationToken);
        return result.ToOkOrProblem();
    }

    private static async Task<IResult> LogoutAsync(
        [FromBody] LogoutRequest request,
        ISender sender,
        ICurrentUserService currentUser,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var command = new LogoutCommand(currentUser.UserId!.Value, request.RefreshToken, GetClientIp(httpContext));
        var result = await sender.Send(command, cancellationToken);
        return result.ToOkOrProblem();
    }

    private static async Task<IResult> ForgotPasswordAsync(
        [FromBody] ForgotPasswordRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new ForgotPasswordCommand(request.Email), cancellationToken);
        return result.ToOkOrProblem();
    }

    private static async Task<IResult> ResetPasswordAsync(
        [FromBody] ResetPasswordRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var command = new ResetPasswordCommand(request.Token, request.NewPassword, request.ConfirmNewPassword);
        var result = await sender.Send(command, cancellationToken);
        return result.ToOkOrProblem();
    }

    private static string GetClientIp(HttpContext httpContext) =>
        httpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault()?.Split(',')[0].Trim()
        ?? httpContext.Connection.RemoteIpAddress?.ToString()
        ?? "unknown";

    private static string? GetUserAgent(HttpContext httpContext) =>
        httpContext.Request.Headers.UserAgent.FirstOrDefault();
}
