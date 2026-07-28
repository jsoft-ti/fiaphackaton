namespace CampaignUserService.Application.Features.Roles.Dtos;

public sealed record RoleDto(Guid Id, string Name, string Description, DateTime CreatedAtUtc);
