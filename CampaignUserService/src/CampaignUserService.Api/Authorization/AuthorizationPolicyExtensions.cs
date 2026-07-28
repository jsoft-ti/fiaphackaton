using CampaignUserService.Domain.Enums;
using Microsoft.AspNetCore.Authorization;

namespace CampaignUserService.Api.Authorization;

public static class AuthorizationPolicyExtensions
{
    public static IServiceCollection AddCampaignUserAuthorizationPolicies(this IServiceCollection services)
    {
        services.AddAuthorizationBuilder()
            .AddPolicy(PolicyNames.AuthenticatedUser, policy => policy.RequireAuthenticatedUser())
            .AddPolicy(PolicyNames.DoadorOnly, policy => policy.RequireRole(RoleName.Doador.ToString()))
            .AddPolicy(PolicyNames.GestorOngOnly, policy => policy.RequireRole(RoleName.GestorOng.ToString()));

        return services;
    }
}
