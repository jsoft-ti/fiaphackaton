using AutoMapper;
using CampaignUserService.Application.Features.Roles.Dtos;
using CampaignUserService.Application.Features.Users.Dtos;
using CampaignUserService.Domain.Entities;

namespace CampaignUserService.Application.Common.Mappings;

public sealed class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<User, UserDto>()
            .ForMember(d => d.Status, opt => opt.MapFrom(s => s.Status.ToString()))
            .ForMember(d => d.Role, opt => opt.MapFrom(s => s.UserRoles
                .Select(ur => ur.Role!.Name.ToString())
                .FirstOrDefault() ?? string.Empty));

        CreateMap<User, UserSummaryDto>()
            .ForMember(d => d.Status, opt => opt.MapFrom(s => s.Status.ToString()))
            .ForMember(d => d.Role, opt => opt.MapFrom(s => s.UserRoles
                .Select(ur => ur.Role!.Name.ToString())
                .FirstOrDefault() ?? string.Empty));

        CreateMap<Role, RoleDto>()
            .ForMember(d => d.Name, opt => opt.MapFrom(s => s.Name.ToString()));
    }
}
