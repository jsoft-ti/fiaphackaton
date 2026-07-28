using CampaignUserService.Application.Common.Interfaces;
using CampaignUserService.Domain.Enums;
using CampaignUserService.Domain.Repositories;
using CampaignUserService.SharedKernel.Common;
using CampaignUserService.SharedKernel.Errors;
using CampaignUserService.SharedKernel.Interfaces;
using FluentValidation;
using MediatR;

namespace CampaignUserService.Application.Features.Users.Commands;

/// <summary>GestorOng-only: PATCH /users/{id}/deactivate.</summary>
public sealed record DeactivateUserCommand(Guid Id, Guid RequestedByUserId) : IRequest<Result>;

public sealed class DeactivateUserCommandValidator : AbstractValidator<DeactivateUserCommand>
{
    public DeactivateUserCommandValidator() => RuleFor(x => x.Id).NotEmpty();
}

public sealed class DeactivateUserCommandHandler(
    IUnitOfWork unitOfWork,
    IAuditService auditService,
    IDateTimeProvider dateTimeProvider) : IRequestHandler<DeactivateUserCommand, Result>
{
    public async Task<Result> Handle(DeactivateUserCommand request, CancellationToken cancellationToken)
    {
        var user = await unitOfWork.Users.GetByIdAsync(request.Id, cancellationToken, includeRoles: false);

        if (user is null || user.IsDeleted)
        {
            return Result.Failure(Error.NotFound("user_not_found", "Usuário não encontrado."));
        }

        var utcNow = dateTimeProvider.UtcNow;
        user.Deactivate(utcNow);

        await unitOfWork.RefreshTokens.RevokeAllActiveForUserAsync(user.Id, utcNow, "system:deactivate", cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        await auditService.LogAsync(
            request.RequestedByUserId,
            AuditActionType.UserDeactivated,
            $"Usuário {user.Email} desativado.",
            cancellationToken);

        return Result.Success();
    }
}
