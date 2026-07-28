using CampaignUserService.Domain.Entities;

namespace CampaignUserService.Domain.Repositories;

public interface IAuditLogRepository
{
    void Add(AuditLog auditLog);

    Task<(IReadOnlyList<AuditLog> Items, int TotalCount)> GetByUserIdAsync(
        Guid userId,
        int page,
        int pageSize,
        CancellationToken cancellationToken);
}
