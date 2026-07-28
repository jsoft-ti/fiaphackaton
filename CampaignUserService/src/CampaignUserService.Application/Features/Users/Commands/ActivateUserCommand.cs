using CampaignUserService.Application.Common.Interfaces;
using CampaignUserService.Domain.Enums;
using CampaignUserService.Domain.Repositories;
using CampaignUserService.SharedKernel.Common;
using CampaignUserService.SharedKernel.Errors;
using CampaignUserService.SharedKernel.Interfaces;
using FluentValidation;
using MediatR;

namespace CampaignUserService.Application.Features.Users.Commands;

/// <summary>GestorOng-only: PATCH /users/{id}/activate.</summary>
public sealed record ActivateUserCommand(Guid Id, Guid RequestedByUserId) : IRequest<Result>;

public sealed class ActivateUserCommandValidator : AbstractValidator<ActivateUserCommand>
{
    public ActivateUserCommandValidator() => RuleFor(x => x.Id).NotEmpty();
}

public sealed class ActivateUserCommandHandler(
    IUnitOfWork unitOfWork,
    IAuditService auditService,
    IDateTimeProvider dateTimeProvider) : IRequestHandler<ActivateUserCommand, Result>
{
    public async Task<Result> Handle(ActivateUserCommand request, CancellationToken cancellationToken)
    {
        var user = await unitOfWork.Users.GetByIdAsync(request.Id, cancellationToken, includeRoles: false);

        if (user is null || user.IsDeleted)
        {
            return Result.Failure(Error.NotFound("user_not_found", "Usuário não encontrado."));
        }

        user.Activate(dateTimeProvider.UtcNow);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        await auditService.LogAsync(
            request.RequestedByUserId,
            AuditActionType.UserActivated,
            $"Usuário {user.Email} ativado.",
            cancellationToken);

        return Result.Success();
    }
}
