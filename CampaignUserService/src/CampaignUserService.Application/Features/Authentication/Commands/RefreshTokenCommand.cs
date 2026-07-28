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

/// <summary>
/// Exchanges a valid, non-revoked refresh token for a new access/refresh
/// token pair. Implements rotation: the old refresh token is revoked and
/// replaced, preventing token replay.
/// </summary>
public sealed record RefreshTokenCommand(
    string RefreshToken,
    string IpAddress,
    string? UserAgent) : IRequest<Result<AuthResultDto>>;

public sealed class RefreshTokenCommandValidator : AbstractValidator<RefreshTokenCommand>
{
    public RefreshTokenCommandValidator()
    {
        RuleFor(x => x.RefreshToken).NotEmpty();
    }
}

public sealed class RefreshTokenCommandHandler(
    IUnitOfWork unitOfWork,
    IJwtTokenService jwtTokenService,
    IAuditService auditService,
    IDateTimeProvider dateTimeProvider) : IRequestHandler<RefreshTokenCommand, Result<AuthResultDto>>
{
    private static readonly Error InvalidTokenError =
        Error.Unauthorized("invalid_refresh_token", "Refresh token inválido ou expirado.");

    public async Task<Result<AuthResultDto>> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        var tokenHash = jwtTokenService.HashRefreshToken(request.RefreshToken);
        var storedToken = await unitOfWork.RefreshTokens.GetByTokenHashAsync(tokenHash, cancellationToken);
        var utcNow = dateTimeProvider.UtcNow;

        if (storedToken is null)
        {
            return Result.Failure<AuthResultDto>(InvalidTokenError);
        }

        if (!storedToken.IsActive(utcNow))
        {
            // Reuse of a revoked/expired token is a strong signal of theft: revoke the whole family.
            if (storedToken.IsRevoked)
            {
                await unitOfWork.RefreshTokens.RevokeAllActiveForUserAsync(
                    storedToken.UserId, utcNow, request.IpAddress, cancellationToken);
                await unitOfWork.SaveChangesAsync(cancellationToken);

                await auditService.LogAsync(
                    storedToken.UserId,
                    AuditActionType.RefreshTokenRevoked,
                    "Reutilização de refresh token detectada. Todos os tokens do usuário foram revogados.",
                    cancellationToken);
            }

            return Result.Failure<AuthResultDto>(InvalidTokenError);
        }

        var user = await unitOfWork.Users.GetByIdAsync(storedToken.UserId, cancellationToken);

        if (user is null || !user.CanAuthenticate())
        {
            return Result.Failure<AuthResultDto>(InvalidTokenError);
        }

        var roleName = user.UserRoles.Select(ur => ur.Role!.Name).FirstOrDefault();

        var newAccessToken = jwtTokenService.GenerateAccessToken(user, roleName.ToString());
        var newRawRefreshToken = jwtTokenService.GenerateRefreshTokenValue();
        var newRefreshTokenHash = jwtTokenService.HashRefreshToken(newRawRefreshToken);

        storedToken.Revoke(utcNow, request.IpAddress, newRefreshTokenHash);

        var newRefreshToken = RefreshToken.Create(
            user.Id,
            newRefreshTokenHash,
            utcNow.Add(jwtTokenService.RefreshTokenLifetime),
            request.IpAddress,
            request.UserAgent);

        unitOfWork.RefreshTokens.Add(newRefreshToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        await auditService.LogAsync(
            user.Id,
            AuditActionType.RefreshTokenIssued,
            "Access token renovado via refresh token.",
            cancellationToken);

        return Result.Success(new AuthResultDto(
            newAccessToken.Token,
            newRawRefreshToken,
            newAccessToken.ExpiresAtUtc,
            "Bearer",
            user.Id,
            user.Email,
            user.FullName,
            roleName.ToString()));
    }
}
