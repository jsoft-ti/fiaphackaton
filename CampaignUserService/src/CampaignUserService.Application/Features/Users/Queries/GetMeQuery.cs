using AutoMapper;
using CampaignUserService.Application.Features.Users.Dtos;
using CampaignUserService.Domain.Repositories;
using CampaignUserService.SharedKernel.Common;
using CampaignUserService.SharedKernel.Errors;
using MediatR;

namespace CampaignUserService.Application.Features.Users.Queries;

public sealed record GetMeQuery(Guid UserId) : IRequest<Result<UserDto>>;

public sealed class GetMeQueryHandler(IUnitOfWork unitOfWork, IMapper mapper)
    : IRequestHandler<GetMeQuery, Result<UserDto>>
{
    public async Task<Result<UserDto>> Handle(GetMeQuery request, CancellationToken cancellationToken)
    {
        var user = await unitOfWork.Users.GetByIdAsync(request.UserId, cancellationToken);

        if (user is null || user.IsDeleted)
        {
            return Result.Failure<UserDto>(Error.NotFound("user_not_found", "Usuário não encontrado."));
        }

        return Result.Success(mapper.Map<UserDto>(user));
    }
}
