using AutoMapper;
using CampaignUserService.Application.Features.Users.Dtos;
using CampaignUserService.Domain.Repositories;
using CampaignUserService.SharedKernel.Common;
using CampaignUserService.SharedKernel.Errors;
using MediatR;

namespace CampaignUserService.Application.Features.Users.Queries;

/// <summary>GestorOng-only lookup of any user by id.</summary>
public sealed record GetUserByIdQuery(Guid Id) : IRequest<Result<UserDto>>;

public sealed class GetUserByIdQueryHandler(IUnitOfWork unitOfWork, IMapper mapper)
    : IRequestHandler<GetUserByIdQuery, Result<UserDto>>
{
    public async Task<Result<UserDto>> Handle(GetUserByIdQuery request, CancellationToken cancellationToken)
    {
        var user = await unitOfWork.Users.GetByIdAsync(request.Id, cancellationToken);

        if (user is null || user.IsDeleted)
        {
            return Result.Failure<UserDto>(Error.NotFound("user_not_found", "Usuário não encontrado."));
        }

        return Result.Success(mapper.Map<UserDto>(user));
    }
}
