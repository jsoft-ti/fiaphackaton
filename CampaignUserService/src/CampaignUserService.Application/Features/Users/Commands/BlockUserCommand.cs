using CampaignUserService.Application.Common.Interfaces;
using CampaignUserService.Domain.Enums;
using CampaignUserService.Domain.Repositories;
using CampaignUserService.SharedKernel.Common;
using CampaignUserService.SharedKernel.Errors;
using CampaignUserService.SharedKernel.Interfaces;
using FluentValidation;
using MediatR;

namespace CampaignUserService.Application.Features.Users.Commands;

/// <summary>GestorOng-only: PATCH /users/{id}/block. Blocks a misbehaving account.</summary>
public sealed record BlockUserCommand(Guid Id, Guid RequestedByUserId) : IRequest<Result>;

public sealed class BlockUserCommandValidator : AbstractValidator<BlockUserCommand>
{
    public BlockUserCommandValidator() => RuleFor(x => x.Id).NotEmpty();
}

public sealed class BlockUserCommandHandler(
    IUnitOfWork unitOfWork,
    IAuditService auditService,
    IDateTimeProvider dateTimeProvider) : IRequestHandler<BlockUserCommand, Result>
{
    public async Task<Result> Handle(BlockUserCommand request, CancellationToken cancellationToken)
    {
        if (request.Id == request.RequestedByUserId)
        {
            return Result.Failure(Error.Validation("cannot_block_self", "Você não pode bloquear a própria conta."));
        }

        var user = await unitOfWork.Users.GetByIdAsync(request.Id, cancellationToken, includeRoles: false);

        if (user is null || user.IsDeleted)
        {
            return Result.Failure(Error.NotFound("user_not_found", "Usuário não encontrado."));
        }

        var utcNow = dateTimeProvider.UtcNow;
        user.Block(utcNow);

        await unitOfWork.RefreshTokens.RevokeAllActiveForUserAsync(user.Id, utcNow, "system:block", cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        await auditService.LogAsync(
            request.RequestedByUserId,
            AuditActionType.UserBlocked,
            $"Usuário {user.Email} bloqueado.",
            cancellationToken);

        return Result.Success();
    }
}
