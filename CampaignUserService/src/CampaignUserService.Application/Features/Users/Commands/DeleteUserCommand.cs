using CampaignUserService.Application.Common.Interfaces;
using CampaignUserService.Domain.Enums;
using CampaignUserService.Domain.Repositories;
using CampaignUserService.SharedKernel.Common;
using CampaignUserService.SharedKernel.Errors;
using CampaignUserService.SharedKernel.Interfaces;
using FluentValidation;
using MediatR;

namespace CampaignUserService.Application.Features.Users.Commands;

/// <summary>GestorOng-only deletion of any user (DELETE /users/{id}). Soft-delete.</summary>
public sealed record DeleteUserCommand(Guid Id, Guid RequestedByUserId) : IRequest<Result>;

public sealed class DeleteUserCommandValidator : AbstractValidator<DeleteUserCommand>
{
    public DeleteUserCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}

public sealed class DeleteUserCommandHandler(
    IUnitOfWork unitOfWork,
    IAuditService auditService,
    IDateTimeProvider dateTimeProvider) : IRequestHandler<DeleteUserCommand, Result>
{
    public async Task<Result> Handle(DeleteUserCommand request, CancellationToken cancellationToken)
    {
        if (request.Id == request.RequestedByUserId)
        {
            return Result.Failure(Error.Validation(
                "cannot_self_delete_via_admin_endpoint",
                "Utilize DELETE /users/me para excluir a própria conta."));
        }

        var user = await unitOfWork.Users.GetByIdAsync(request.Id, cancellationToken, includeRoles: false);

        if (user is null || user.IsDeleted)
        {
            return Result.Failure(Error.NotFound("user_not_found", "Usuário não encontrado."));
        }

        var utcNow = dateTimeProvider.UtcNow;
        user.SoftDelete(utcNow);
        user.Deactivate(utcNow);

        await unitOfWork.RefreshTokens.RevokeAllActiveForUserAsync(user.Id, utcNow, "system:admin-delete", cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        await auditService.LogAsync(
            request.RequestedByUserId,
            AuditActionType.UserDeleted,
            $"Usuário {user.Email} excluído por um GestorOng.",
            cancellationToken);

        return Result.Success();
    }
}
