using AutoMapper;
using DonationService.Application.Features.Donations.DTOs;
using DonationService.Domain.Repositories;
using DonationService.SharedKernel.Common;
using DonationService.SharedKernel.Errors;
using DonationService.SharedKernel.Interfaces;
using FluentValidation;
using MediatR;

namespace DonationService.Application.Features.Donations.Queries.GetDonationById;

/// <summary>
/// Reads from the MongoDB read model (eventually consistent - populated
/// asynchronously by the Worker after the DonationCreatedEvent is consumed).
/// A donation created moments ago may briefly return NotFound until the
/// Worker catches up; this is a deliberate, documented trade-off of the
/// event-driven design.
/// </summary>
public sealed record GetDonationByIdQuery(Guid DonationId) : IRequest<Result<DonationDto>>;

public sealed class GetDonationByIdQueryValidator : AbstractValidator<GetDonationByIdQuery>
{
    public GetDonationByIdQueryValidator()
    {
        RuleFor(x => x.DonationId).NotEmpty();
    }
}

public sealed class GetDonationByIdQueryHandler : IRequestHandler<GetDonationByIdQuery, Result<DonationDto>>
{
    private const string GestorOngRole = "GestorOng";

    private readonly IDonationReadRepository _readRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IMapper _mapper;

    public GetDonationByIdQueryHandler(
        IDonationReadRepository readRepository,
        ICurrentUserService currentUserService,
        IMapper mapper)
    {
        _readRepository = readRepository;
        _currentUserService = currentUserService;
        _mapper = mapper;
    }

    public async Task<Result<DonationDto>> Handle(GetDonationByIdQuery request, CancellationToken cancellationToken)
    {
        var donation = await _readRepository.GetByIdAsync(request.DonationId, cancellationToken);

        if (donation is null)
        {
            return Result.Failure<DonationDto>(
                Error.NotFound("donation_not_found", $"Donation '{request.DonationId}' was not found."));
        }

        var isOwner = _currentUserService.UserId == donation.UserId;
        var isGestor = string.Equals(_currentUserService.Role, GestorOngRole, StringComparison.OrdinalIgnoreCase);

        if (!isOwner && !isGestor)
        {
            return Result.Failure<DonationDto>(
                Error.Forbidden("donation_access_denied", "You are not allowed to view this donation."));
        }

        return Result.Success(_mapper.Map<DonationDto>(donation));
    }
}
