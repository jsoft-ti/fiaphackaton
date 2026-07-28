using CampaignUserService.Domain.Enums;

namespace CampaignUserService.Api.Contracts;

public sealed record CreateRoleRequest(RoleName Name, string Description);
