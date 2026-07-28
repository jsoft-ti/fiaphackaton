using AutoMapper;
using CampaignUserService.Application.Common.Interfaces;
using CampaignUserService.Application.Features.Roles.Dtos;
using CampaignUserService.Domain.Entities;
using CampaignUserService.Domain.Enums;
using CampaignUserService.Domain.Repositories;
using CampaignUserService.SharedKernel.Common;
using CampaignUserService.SharedKernel.Errors;
using FluentValidation;
using MediatR;

namespace CampaignUserService.Application.Features.Roles.Commands;

/// <summary>
/// GestorOng-only. The role set is intentionally closed (Doador / GestorOng),
/// this endpoint exists to satisfy the roles CRUD contract and allows
/// updating the description of an existing role name idempotently.
/// </summary>
public sealed record CreateRoleCommand(RoleName Name, string Description) : IRequest<Result<RoleDto>>;

public sealed class CreateRoleCommandValidator : AbstractValidator<CreateRoleCommand>
{
    public CreateRoleCommandValidator()
    {
        RuleFor(x => x.Name).IsInEnum();
        RuleFor(x => x.Description).NotEmpty().MaximumLength(500);
    }
}

public sealed class CreateRoleCommandHandler(
    IUnitOfWork unitOfWork,
    IMapper mapper,
    IAuditService auditService) : IRequestHandler<CreateRoleCommand, Result<RoleDto>>
{
    public async Task<Result<RoleDto>> Handle(CreateRoleCommand request, CancellationToken cancellationToken)
    {
        if (await unitOfWork.Roles.ExistsByNameAsync(request.Name, cancellationToken))
        {
            return Result.Failure<RoleDto>(
                Error.Conflict("role_already_exists", $"A role '{request.Name}' já está cadastrada."));
        }

        var role = Role.Create(request.Name, request.Description);
        unitOfWork.Roles.Add(role);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        await auditService.LogAsync(
            null,
            AuditActionType.RoleCreated,
            $"Role '{role.Name}' criada.",
            cancellationToken);

        return Result.Success(mapper.Map<RoleDto>(role));
    }
}
