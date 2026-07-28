using AutoMapper;
using DonationService.Application.Common.Models;
using DonationService.Application.Features.Donations.DTOs;
using DonationService.Domain.Repositories;
using DonationService.SharedKernel.Common;
using DonationService.SharedKernel.Errors;
using DonationService.SharedKernel.Interfaces;
using FluentValidation;
using MediatR;

namespace DonationService.Application.Features.Donations.Queries.GetMyDonations;

public sealed record GetMyDonationsQuery(int Page = 1, int PageSize = 20) : IRequest<Result<PagedResult<DonationDto>>>;

public sealed class GetMyDonationsQueryValidator : AbstractValidator<GetMyDonationsQuery>
{
    public GetMyDonationsQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
    }
}

public sealed class GetMyDonationsQueryHandler
    : IRequestHandler<GetMyDonationsQuery, Result<PagedResult<DonationDto>>>
{
    private readonly IDonationReadRepository _readRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IMapper _mapper;

    public GetMyDonationsQueryHandler(
        IDonationReadRepository readRepository,
        ICurrentUserService currentUserService,
        IMapper mapper)
    {
        _readRepository = readRepository;
        _currentUserService = currentUserService;
        _mapper = mapper;
    }

    public async Task<Result<PagedResult<DonationDto>>> Handle(
        GetMyDonationsQuery request,
        CancellationToken cancellationToken)
    {
        if (!_currentUserService.UserId.HasValue || _currentUserService.UserId.Value == Guid.Empty)
        {
            return Result.Failure<PagedResult<DonationDto>>(
                Error.Unauthorized("unauthenticated", "An authenticated user is required."));
        }

        var (items, totalCount) = await _readRepository.GetByUserIdAsync(
            _currentUserService.UserId.Value, request.Page, request.PageSize, cancellationToken);

        var dtos = items.Select(_mapper.Map<DonationDto>).ToList();

        return Result.Success(new PagedResult<DonationDto>(dtos, request.Page, request.PageSize, totalCount));
    }
}
