using CampaignUserService.Domain.Entities;
using CampaignUserService.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace CampaignUserService.Infrastructure.Persistence.Repositories;

public sealed class AuditLogRepository(ApplicationDbContext dbContext) : IAuditLogRepository
{
    public void Add(AuditLog auditLog) => dbContext.AuditLogs.Add(auditLog);

    public async Task<(IReadOnlyList<AuditLog> Items, int TotalCount)> GetByUserIdAsync(
        Guid userId,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var query = dbContext.AuditLogs.Where(a => a.UserId == userId);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(a => a.OccurredAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }
}
