namespace CampaignUserService.Api.Authorization;

/// <summary>
/// Central catalog of RBAC authorization policy names. Endpoints must always
/// reference a policy (via <c>RequireAuthorization(PolicyNames.X)</c>) -
/// manual role checks (<c>User.IsInRole(...)</c>) are not allowed anywhere
/// in the Api layer.
/// </summary>
public static class PolicyNames
{
    /// <summary>Any authenticated user, regardless of role (Doador or GestorOng).</summary>
    public const string AuthenticatedUser = "AuthenticatedUser";

    /// <summary>Restricted to users with the Doador role.</summary>
    public const string DoadorOnly = "DoadorOnly";

    /// <summary>Restricted to users with the GestorOng role - administrative operations.</summary>
    public const string GestorOngOnly = "GestorOngOnly";
}
