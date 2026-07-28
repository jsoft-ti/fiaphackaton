using CampaignUserService.Application.Common.Interfaces;
using CampaignUserService.Domain.Enums;
using CampaignUserService.Domain.Repositories;
using CampaignUserService.SharedKernel.Common;
using CampaignUserService.SharedKernel.Errors;
using CampaignUserService.SharedKernel.Interfaces;
using FluentValidation;
using MediatR;

namespace CampaignUserService.Application.Features.Users.Commands;

/// <summary>GestorOng-only: PATCH /users/{id}/roles.</summary>
public sealed record ChangeUserRoleCommand(Guid Id, RoleName NewRole, Guid RequestedByUserId) : IRequest<Result>;

public sealed class ChangeUserRoleCommandValidator : AbstractValidator<ChangeUserRoleCommand>
{
    public ChangeUserRoleCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.NewRole).IsInEnum();
    }
}

public sealed class ChangeUserRoleCommandHandler(
    IUnitOfWork unitOfWork,
    IAuditService auditService,
    IDateTimeProvider dateTimeProvider) : IRequestHandler<ChangeUserRoleCommand, Result>
{
    public async Task<Result> Handle(ChangeUserRoleCommand request, CancellationToken cancellationToken)
    {
        if (request.Id == request.RequestedByUserId)
        {
            return Result.Failure(Error.Validation(
                "cannot_change_own_role",
                "Você não pode alterar a própria role."));
        }

        var user = await unitOfWork.Users.GetByIdAsync(request.Id, cancellationToken);

        if (user is null || user.IsDeleted)
        {
            return Result.Failure(Error.NotFound("user_not_found", "Usuário não encontrado."));
        }

        var newRole = await unitOfWork.Roles.GetByNameAsync(request.NewRole, cancellationToken)
            ?? throw new InvalidOperationException($"A role '{request.NewRole}' não está cadastrada.");

        var utcNow = dateTimeProvider.UtcNow;
        user.ReplaceRole(newRole, utcNow);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        await auditService.LogAsync(
            request.RequestedByUserId,
            AuditActionType.RoleChanged,
            $"Role do usuário {user.Email} alterada para {newRole.Name}.",
            cancellationToken);

        return Result.Success();
    }
}
