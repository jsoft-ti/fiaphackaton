namespace CampaignUserService.Application.Common.Models;

/// <summary>
/// Configuration for the initial GestorOng administrator created by the
/// database seeder (Options Pattern, bound from "AdminSeed" section /
/// environment variables - never hardcoded).
/// </summary>
public sealed class AdminSeedSettings
{
    public const string SectionName = "AdminSeed";

    public string FirstName { get; init; } = "Admin";

    public string LastName { get; init; } = "Master";

    public string Email { get; init; } = string.Empty;

    public string Password { get; init; } = string.Empty;
}
