using CampaignUserService.Application.Common.Interfaces;
using CampaignUserService.Application.Features.Authentication.Dtos;
using CampaignUserService.Domain.Entities;
using CampaignUserService.Domain.Enums;
using CampaignUserService.Domain.Repositories;
using CampaignUserService.SharedKernel.Common;
using CampaignUserService.SharedKernel.Errors;
using CampaignUserService.SharedKernel.Interfaces;
using FluentValidation;
using MediatR;

namespace CampaignUserService.Application.Features.Authentication.Commands;

public sealed record LoginCommand(
    string Email,
    string Password,
    string IpAddress,
    string? UserAgent) : IRequest<Result<AuthResultDto>>;

public sealed class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty();
    }
}

public sealed class LoginCommandHandler(
    IUnitOfWork unitOfWork,
    IPasswordHasher passwordHasher,
    IJwtTokenService jwtTokenService,
    IAuditService auditService,
    IDateTimeProvider dateTimeProvider) : IRequestHandler<LoginCommand, Result<AuthResultDto>>
{
    private static readonly Error InvalidCredentialsError =
        Error.Unauthorized("invalid_credentials", "Email ou senha inválidos.");

    public async Task<Result<AuthResultDto>> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var user = await unitOfWork.Users.GetByEmailAsync(request.Email, cancellationToken);

        if (user is null || !passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            if (user is not null)
            {
                user.RecordFailedLoginAttempt(dateTimeProvider.UtcNow);
                await unitOfWork.SaveChangesAsync(cancellationToken);
            }

            await auditService.LogAsync(
                user?.Id,
                AuditActionType.UserLoggedIn,
                $"Tentativa de login inválida para o email {request.Email}.",
                cancellationToken);

            return Result.Failure<AuthResultDto>(InvalidCredentialsError);
        }

        if (user.Status == UserStatus.Blocked)
        {
            return Result.Failure<AuthResultDto>(
                Error.Forbidden("user_blocked", "Esta conta está bloqueada. Entre em contato com o suporte."));
        }

        if (user.Status == UserStatus.Inactive)
        {
            return Result.Failure<AuthResultDto>(
                Error.Forbidden("user_inactive", "Esta conta está inativa."));
        }

        var roleName = user.UserRoles.Select(ur => ur.Role!.Name).FirstOrDefault();

        user.RecordSuccessfulLogin(dateTimeProvider.UtcNow);

        var accessToken = jwtTokenService.GenerateAccessToken(user, roleName.ToString());
        var rawRefreshToken = jwtTokenService.GenerateRefreshTokenValue();
        var refreshTokenHash = jwtTokenService.HashRefreshToken(rawRefreshToken);

        var refreshToken = RefreshToken.Create(
            user.Id,
            refreshTokenHash,
            dateTimeProvider.UtcNow.Add(jwtTokenService.RefreshTokenLifetime),
            request.IpAddress,
            request.UserAgent);

        unitOfWork.RefreshTokens.Add(refreshToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        await auditService.LogAsync(
            user.Id,
            AuditActionType.UserLoggedIn,
            $"Usuário {user.Email} autenticado com sucesso.",
            cancellationToken);

        return Result.Success(new AuthResultDto(
            accessToken.Token,
            rawRefreshToken,
            accessToken.ExpiresAtUtc,
            "Bearer",
            user.Id,
            user.Email,
            user.FullName,
            roleName.ToString()));
    }
}
