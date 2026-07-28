using CampaignUserService.Application.Common.Interfaces;
using CampaignUserService.Domain.Enums;
using CampaignUserService.Domain.Repositories;
using CampaignUserService.SharedKernel.Common;
using CampaignUserService.SharedKernel.Errors;
using CampaignUserService.SharedKernel.Interfaces;
using FluentValidation;
using MediatR;

namespace CampaignUserService.Application.Features.Users.Commands;

/// <summary>Self-service account deletion (DELETE /users/me). Soft-delete, revokes all sessions.</summary>
public sealed record DeleteMeCommand(Guid UserId) : IRequest<Result>;

public sealed class DeleteMeCommandValidator : AbstractValidator<DeleteMeCommand>
{
    public DeleteMeCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
    }
}

public sealed class DeleteMeCommandHandler(
    IUnitOfWork unitOfWork,
    IAuditService auditService,
    IDateTimeProvider dateTimeProvider) : IRequestHandler<DeleteMeCommand, Result>
{
    public async Task<Result> Handle(DeleteMeCommand request, CancellationToken cancellationToken)
    {
        var user = await unitOfWork.Users.GetByIdAsync(request.UserId, cancellationToken, includeRoles: false);

        if (user is null || user.IsDeleted)
        {
            return Result.Failure(Error.NotFound("user_not_found", "Usuário não encontrado."));
        }

        var utcNow = dateTimeProvider.UtcNow;
        user.SoftDelete(utcNow);
        user.Deactivate(utcNow);

        await unitOfWork.RefreshTokens.RevokeAllActiveForUserAsync(user.Id, utcNow, "system:self-delete", cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        await auditService.LogAsync(
            user.Id,
            AuditActionType.UserDeleted,
            "Usuário excluiu a própria conta.",
            cancellationToken);

        return Result.Success();
    }
}
