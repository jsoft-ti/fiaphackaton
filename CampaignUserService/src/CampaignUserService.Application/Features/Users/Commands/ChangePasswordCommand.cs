using CampaignUserService.Application.Common.Interfaces;
using CampaignUserService.Domain.Enums;
using CampaignUserService.Domain.Repositories;
using CampaignUserService.SharedKernel.Common;
using CampaignUserService.SharedKernel.Errors;
using CampaignUserService.SharedKernel.Interfaces;
using FluentValidation;
using MediatR;

namespace CampaignUserService.Application.Features.Users.Commands;

/// <summary>Self-service password change - requires the current password.</summary>
public sealed record ChangePasswordCommand(
    Guid UserId,
    string CurrentPassword,
    string NewPassword,
    string ConfirmNewPassword) : IRequest<Result>;

public sealed class ChangePasswordCommandValidator : AbstractValidator<ChangePasswordCommand>
{
    public ChangePasswordCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.CurrentPassword).NotEmpty();

        RuleFor(x => x.NewPassword)
            .NotEmpty()
            .MinimumLength(8)
            .Matches("[A-Z]").WithMessage("A senha deve conter ao menos uma letra maiúscula.")
            .Matches("[a-z]").WithMessage("A senha deve conter ao menos uma letra minúscula.")
            .Matches("[0-9]").WithMessage("A senha deve conter ao menos um número.")
            .Matches("[^a-zA-Z0-9]").WithMessage("A senha deve conter ao menos um caractere especial.")
            .NotEqual(x => x.CurrentPassword).WithMessage("A nova senha deve ser diferente da senha atual.");

        RuleFor(x => x.ConfirmNewPassword)
            .Equal(x => x.NewPassword)
            .WithMessage("As senhas não coincidem.");
    }
}

public sealed class ChangePasswordCommandHandler(
    IUnitOfWork unitOfWork,
    IPasswordHasher passwordHasher,
    IAuditService auditService,
    IDateTimeProvider dateTimeProvider) : IRequestHandler<ChangePasswordCommand, Result>
{
    public async Task<Result> Handle(ChangePasswordCommand request, CancellationToken cancellationToken)
    {
        var user = await unitOfWork.Users.GetByIdAsync(request.UserId, cancellationToken, includeRoles: false);

        if (user is null || user.IsDeleted)
        {
            return Result.Failure(Error.NotFound("user_not_found", "Usuário não encontrado."));
        }

        if (!passwordHasher.Verify(request.CurrentPassword, user.PasswordHash))
        {
            return Result.Failure(Error.Validation("invalid_current_password", "A senha atual informada está incorreta."));
        }

        var utcNow = dateTimeProvider.UtcNow;
        user.ChangePassword(passwordHasher.Hash(request.NewPassword), utcNow);

        await unitOfWork.RefreshTokens.RevokeAllActiveForUserAsync(user.Id, utcNow, "system:change-password", cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        await auditService.LogAsync(
            user.Id,
            AuditActionType.PasswordChanged,
            "Usuário alterou a própria senha.",
            cancellationToken);

        return Result.Success();
    }
}
