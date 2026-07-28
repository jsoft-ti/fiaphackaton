using System.Security.Cryptography;
using CampaignUserService.Application.Common.Interfaces;
using CampaignUserService.Domain.Entities;
using CampaignUserService.Domain.Enums;
using CampaignUserService.Domain.Repositories;
using CampaignUserService.SharedKernel.Common;
using CampaignUserService.SharedKernel.Errors;
using CampaignUserService.SharedKernel.Interfaces;
using FluentValidation;
using MediatR;

namespace CampaignUserService.Application.Features.Users.Commands;

/// <summary>
/// GestorOng-only forced password reset. Does not set the password directly
/// (that would require transmitting a plaintext password); instead it
/// generates a password-reset token and emails it to the user, reusing the
/// same secure flow as the self-service "forgot password".
/// </summary>
public sealed record AdminResetPasswordCommand(Guid Id, Guid RequestedByUserId) : IRequest<Result>;

public sealed class AdminResetPasswordCommandValidator : AbstractValidator<AdminResetPasswordCommand>
{
    public AdminResetPasswordCommandValidator() => RuleFor(x => x.Id).NotEmpty();
}

public sealed class AdminResetPasswordCommandHandler(
    IUnitOfWork unitOfWork,
    IJwtTokenService jwtTokenService,
    IEmailSender emailSender,
    IAuditService auditService,
    IDateTimeProvider dateTimeProvider) : IRequestHandler<AdminResetPasswordCommand, Result>
{
    private static readonly TimeSpan TokenLifetime = TimeSpan.FromHours(1);

    public async Task<Result> Handle(AdminResetPasswordCommand request, CancellationToken cancellationToken)
    {
        var user = await unitOfWork.Users.GetByIdAsync(request.Id, cancellationToken, includeRoles: false);

        if (user is null || user.IsDeleted)
        {
            return Result.Failure(Error.NotFound("user_not_found", "Usuário não encontrado."));
        }

        await unitOfWork.PasswordResetTokens.InvalidateActiveTokensForUserAsync(user.Id, cancellationToken);

        var rawToken = GenerateSecureToken();
        var tokenHash = jwtTokenService.HashRefreshToken(rawToken);
        var utcNow = dateTimeProvider.UtcNow;

        var resetToken = PasswordResetToken.Create(user.Id, tokenHash, utcNow.Add(TokenLifetime));
        unitOfWork.PasswordResetTokens.Add(resetToken);

        await unitOfWork.RefreshTokens.RevokeAllActiveForUserAsync(user.Id, utcNow, "system:admin-reset-password", cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        await emailSender.SendPasswordResetEmailAsync(user.Email, user.FullName, rawToken, cancellationToken);

        await auditService.LogAsync(
            request.RequestedByUserId,
            AuditActionType.PasswordResetRequested,
            $"Reset de senha do usuário {user.Email} forçado por um GestorOng.",
            cancellationToken);

        return Result.Success();
    }

    private static string GenerateSecureToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return Convert.ToBase64String(bytes)
            .Replace("+", "-")
            .Replace("/", "_")
            .Replace("=", string.Empty);
    }
}
