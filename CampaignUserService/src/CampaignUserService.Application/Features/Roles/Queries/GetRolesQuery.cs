using AutoMapper;
using CampaignUserService.Application.Features.Roles.Dtos;
using CampaignUserService.Domain.Repositories;
using CampaignUserService.SharedKernel.Common;
using MediatR;

namespace CampaignUserService.Application.Features.Roles.Queries;

public sealed record GetRolesQuery : IRequest<Result<IReadOnlyList<RoleDto>>>;

public sealed class GetRolesQueryHandler(IUnitOfWork unitOfWork, IMapper mapper)
    : IRequestHandler<GetRolesQuery, Result<IReadOnlyList<RoleDto>>>
{
    public async Task<Result<IReadOnlyList<RoleDto>>> Handle(GetRolesQuery request, CancellationToken cancellationToken)
    {
        var roles = await unitOfWork.Roles.GetAllAsync(cancellationToken);
        var dtos = roles.Select(mapper.Map<RoleDto>).ToList();
        return Result.Success<IReadOnlyList<RoleDto>>(dtos);
    }
}
