using CampaignUserService.Application.Common.Interfaces;
using CampaignUserService.Domain.Enums;
using CampaignUserService.Domain.Repositories;
using CampaignUserService.SharedKernel.Common;
using CampaignUserService.SharedKernel.Errors;
using CampaignUserService.SharedKernel.Interfaces;
using FluentValidation;
using MediatR;

namespace CampaignUserService.Application.Features.Authentication.Commands;

public sealed record ResetPasswordCommand(
    string Token,
    string NewPassword,
    string ConfirmNewPassword) : IRequest<Result>;

public sealed class ResetPasswordCommandValidator : AbstractValidator<ResetPasswordCommand>
{
    public ResetPasswordCommandValidator()
    {
        RuleFor(x => x.Token).NotEmpty();

        RuleFor(x => x.NewPassword)
            .NotEmpty()
            .MinimumLength(8)
            .Matches("[A-Z]").WithMessage("A senha deve conter ao menos uma letra maiúscula.")
            .Matches("[a-z]").WithMessage("A senha deve conter ao menos uma letra minúscula.")
            .Matches("[0-9]").WithMessage("A senha deve conter ao menos um número.")
            .Matches("[^a-zA-Z0-9]").WithMessage("A senha deve conter ao menos um caractere especial.");

        RuleFor(x => x.ConfirmNewPassword)
            .Equal(x => x.NewPassword)
            .WithMessage("As senhas não coincidem.");
    }
}

public sealed class ResetPasswordCommandHandler(
    IUnitOfWork unitOfWork,
    IPasswordHasher passwordHasher,
    IJwtTokenService jwtTokenService,
    IAuditService auditService,
    IDateTimeProvider dateTimeProvider) : IRequestHandler<ResetPasswordCommand, Result>
{
    private static readonly Error InvalidTokenError =
        Error.Validation("invalid_reset_token", "Token de redefinição de senha inválido ou expirado.");

    public async Task<Result> Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
    {
        var tokenHash = jwtTokenService.HashRefreshToken(request.Token);
        var resetToken = await unitOfWork.PasswordResetTokens.GetByTokenHashAsync(tokenHash, cancellationToken);
        var utcNow = dateTimeProvider.UtcNow;

        if (resetToken is null || !resetToken.IsValid(utcNow))
        {
            return Result.Failure(InvalidTokenError);
        }

        var user = await unitOfWork.Users.GetByIdAsync(resetToken.UserId, cancellationToken, includeRoles: false);

        if (user is null)
        {
            return Result.Failure(InvalidTokenError);
        }

        user.ChangePassword(passwordHasher.Hash(request.NewPassword), utcNow);
        resetToken.MarkUsed(utcNow);

        // A password reset invalidates every existing session for safety.
        await unitOfWork.RefreshTokens.RevokeAllActiveForUserAsync(user.Id, utcNow, "system:password-reset", cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        await auditService.LogAsync(
            user.Id,
            AuditActionType.PasswordResetCompleted,
            "Senha redefinida com sucesso via token de recuperação.",
            cancellationToken);

        return Result.Success();
    }
}
