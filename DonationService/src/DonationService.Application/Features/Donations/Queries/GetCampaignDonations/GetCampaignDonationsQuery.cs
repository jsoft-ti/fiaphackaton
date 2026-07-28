using AutoMapper;
using DonationService.Application.Common.Models;
using DonationService.Application.Features.Donations.DTOs;
using DonationService.Domain.Repositories;
using DonationService.SharedKernel.Common;
using DonationService.SharedKernel.Errors;
using DonationService.SharedKernel.Interfaces;
using FluentValidation;
using MediatR;

namespace DonationService.Application.Features.Donations.Queries.GetCampaignDonations;

/// <summary>
/// Reserved for a future GestorOng-facing endpoint/dashboard (not yet
/// wired to a controller route in this iteration, per the current API
/// surface: POST /donations, GET /donations/{id}, GET /donations/me). The
/// handler and its GestorOng-only authorization rule are implemented now so
/// the feature can be exposed later with no Application-layer changes.
/// </summary>
public sealed record GetCampaignDonationsQuery(
    Guid CampaignId,
    int Page = 1,
    int PageSize = 20) : IRequest<Result<PagedResult<DonationDto>>>;

public sealed class GetCampaignDonationsQueryValidator : AbstractValidator<GetCampaignDonationsQuery>
{
    public GetCampaignDonationsQueryValidator()
    {
        RuleFor(x => x.CampaignId).NotEmpty();
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
    }
}

public sealed class GetCampaignDonationsQueryHandler
    : IRequestHandler<GetCampaignDonationsQuery, Result<PagedResult<DonationDto>>>
{
    private const string GestorOngRole = "GestorOng";

    private readonly IDonationReadRepository _readRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IMapper _mapper;

    public GetCampaignDonationsQueryHandler(
        IDonationReadRepository readRepository,
        ICurrentUserService currentUserService,
        IMapper mapper)
    {
        _readRepository = readRepository;
        _currentUserService = currentUserService;
        _mapper = mapper;
    }

    public async Task<Result<PagedResult<DonationDto>>> Handle(
        GetCampaignDonationsQuery request,
        CancellationToken cancellationToken)
    {
        if (!string.Equals(_currentUserService.Role, GestorOngRole, StringComparison.OrdinalIgnoreCase))
        {
            return Result.Failure<PagedResult<DonationDto>>(
                Error.Forbidden("campaign_donations_access_denied", "Only GestorOng may list campaign donations."));
        }

        var (items, totalCount) = await _readRepository.GetByCampaignIdAsync(
            request.CampaignId, request.Page, request.PageSize, cancellationToken);

        var dtos = items.Select(_mapper.Map<DonationDto>).ToList();

        return Result.Success(new PagedResult<DonationDto>(dtos, request.Page, request.PageSize, totalCount));
    }
}
