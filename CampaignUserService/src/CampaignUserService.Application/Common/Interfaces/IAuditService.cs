using CampaignUserService.Domain.Enums;

namespace CampaignUserService.Application.Common.Interfaces;

/// <summary>
/// Records security-sensitive events (login, logout, CRUD on users, role
/// changes, password changes) into the audit trail. Implementations must
/// never throw - auditing failures should never break the main flow.
/// </summary>
public interface IAuditService
{
    Task LogAsync(Guid? userId, AuditActionType action, string description, CancellationToken cancellationToken);
}
