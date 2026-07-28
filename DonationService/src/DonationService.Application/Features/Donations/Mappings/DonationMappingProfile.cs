using AutoMapper;
using DonationService.Application.Features.Donations.DTOs;
using DonationService.Domain.ReadModels;

namespace DonationService.Application.Features.Donations.Mappings;

public sealed class DonationMappingProfile : Profile
{
    public DonationMappingProfile()
    {
        CreateMap<DonationReadModel, DonationDto>();
    }
}
