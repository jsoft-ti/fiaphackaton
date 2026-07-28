using CampaignUserService.Domain.Enums;
using CampaignUserService.Domain.Exceptions;
using CampaignUserService.SharedKernel.Common;

namespace CampaignUserService.Domain.Entities;

/// <summary>
/// Represents an authorization role (Doador / GestorOng). Kept as an entity
/// (rather than a bare enum) so the RBAC model can evolve (e.g. new roles,
/// custom permissions per role) without breaking the schema.
/// </summary>
public class Role : BaseEntity
{
    private readonly List<UserRole> _userRoles = [];

    protected Role()
    {
    }

    private Role(RoleName name, string description)
    {
        Name = name;
        Description = description;
    }

    public RoleName Name { get; private set; }

    public string Description { get; private set; } = string.Empty;

    public IReadOnlyCollection<UserRole> UserRoles => _userRoles.AsReadOnly();

    public static Role Create(RoleName name, string description)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            throw new DomainException("A descrição da role é obrigatória.");
        }

        return new Role(name, description);
    }
}
