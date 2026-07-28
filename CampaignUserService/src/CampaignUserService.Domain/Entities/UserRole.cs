using CampaignUserService.SharedKernel.Common;

namespace CampaignUserService.Domain.Entities;

/// <summary>
/// Join entity between <see cref="User"/> and <see cref="Role"/>. Modeled
/// explicitly (rather than a plain many-to-many) to keep the door open for
/// per-assignment metadata (e.g. AssignedAtUtc, AssignedBy) and audit.
/// </summary>
public class UserRole : BaseEntity
{
    protected UserRole()
    {
    }

    private UserRole(Guid userId, Guid roleId)
    {
        UserId = userId;
        RoleId = roleId;
    }

    public Guid UserId { get; private set; }

    public User? User { get; private set; }

    public Guid RoleId { get; private set; }

    public Role? Role { get; private set; }

    public static UserRole Create(Guid userId, Guid roleId) => new(userId, roleId);
}
