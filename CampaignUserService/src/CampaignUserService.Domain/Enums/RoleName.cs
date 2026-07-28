namespace CampaignUserService.Domain.Enums;

/// <summary>
/// The two roles supported by the system. Kept as a closed set on purpose:
/// the business only recognizes these two profiles today.
/// </summary>
public enum RoleName
{
    Doador = 1,
    GestorOng = 2
}
