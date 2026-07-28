using AutoMapper;
using CampaignUserService.Application.Common.Models;
using CampaignUserService.Application.Features.Users.Dtos;
using CampaignUserService.Domain.Enums;
using CampaignUserService.Domain.Repositories;
using CampaignUserService.SharedKernel.Common;
using FluentValidation;
using MediatR;

namespace CampaignUserService.Application.Features.Users.Queries;

/// <summary>GestorOng-only paginated user listing with search/filter support.</summary>
public sealed record GetUsersQuery(
    string? SearchTerm,
    RoleName? Role,
    UserStatus? Status,
    int Page = 1,
    int PageSize = 20) : IRequest<Result<PagedResult<UserSummaryDto>>>;

public sealed class GetUsersQueryValidator : AbstractValidator<GetUsersQuery>
{
    public GetUsersQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
    }
}

public sealed class GetUsersQueryHandler(IUnitOfWork unitOfWork, IMapper mapper)
    : IRequestHandler<GetUsersQuery, Result<PagedResult<UserSummaryDto>>>
{
    public async Task<Result<PagedResult<UserSummaryDto>>> Handle(GetUsersQuery request, CancellationToken cancellationToken)
    {
        var (items, totalCount) = await unitOfWork.Users.SearchAsync(
            request.SearchTerm,
            request.Role,
            request.Status,
            request.Page,
            request.PageSize,
            cancellationToken);

        var dtos = items.Select(mapper.Map<UserSummaryDto>).ToList();

        return Result.Success(PagedResult<UserSummaryDto>.Create(dtos, totalCount, request.Page, request.PageSize));
    }
}
