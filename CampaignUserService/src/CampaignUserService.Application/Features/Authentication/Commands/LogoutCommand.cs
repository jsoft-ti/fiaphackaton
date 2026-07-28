using CampaignUserService.Application.Common.Interfaces;
using CampaignUserService.Domain.Enums;
using CampaignUserService.Domain.Repositories;
using CampaignUserService.SharedKernel.Common;
using CampaignUserService.SharedKernel.Errors;
using CampaignUserService.SharedKernel.Interfaces;
using FluentValidation;
using MediatR;

namespace CampaignUserService.Application.Features.Authentication.Commands;

/// <summary>Revokes the given refresh token, effectively logging the session out.</summary>
public sealed record LogoutCommand(Guid UserId, string RefreshToken, string IpAddress) : IRequest<Result>;

public sealed class LogoutCommandValidator : AbstractValidator<LogoutCommand>
{
    public LogoutCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.RefreshToken).NotEmpty();
    }
}

public sealed class LogoutCommandHandler(
    IUnitOfWork unitOfWork,
    IJwtTokenService jwtTokenService,
    IAuditService auditService,
    IDateTimeProvider dateTimeProvider) : IRequestHandler<LogoutCommand, Result>
{
    public async Task<Result> Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        var tokenHash = jwtTokenService.HashRefreshToken(request.RefreshToken);
        var storedToken = await unitOfWork.RefreshTokens.GetByTokenHashAsync(tokenHash, cancellationToken);

        if (storedToken is null || storedToken.UserId != request.UserId)
        {
            // Idempotent: logging out with an already invalid/foreign token is not an error.
            return Result.Success();
        }

        if (storedToken.IsActive(dateTimeProvider.UtcNow))
        {
            storedToken.Revoke(dateTimeProvider.UtcNow, request.IpAddress);
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }

        await auditService.LogAsync(
            request.UserId,
            AuditActionType.UserLoggedOut,
            "Usuário efetuou logout.",
            cancellationToken);

        return Result.Success();
    }
}
