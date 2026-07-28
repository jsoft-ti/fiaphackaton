using CampaignUserService.Application.Common.Interfaces;
using CampaignUserService.Domain.Entities;
using CampaignUserService.Domain.Enums;
using CampaignUserService.Domain.Repositories;
using CampaignUserService.Infrastructure.Persistence;
using CampaignUserService.SharedKernel.Interfaces;
using Microsoft.Extensions.Logging;

namespace CampaignUserService.Infrastructure.Services;

/// <summary>
/// Persists audit trail entries directly through the DbContext (independent
/// SaveChanges call) so an audit record is written even when the caller's
/// unit-of-work transaction later fails for unrelated reasons, and enriches
/// each entry with the caller's IP/User-Agent. Never throws.
/// </summary>
public sealed class AuditService(
    ApplicationDbContext dbContext,
    ICurrentUserService currentUserService,
    ILogger<AuditService> logger) : IAuditService
{
    public async Task LogAsync(Guid? userId, AuditActionType action, string description, CancellationToken cancellationToken)
    {
        try
        {
            var entry = AuditLog.Create(
                userId,
                action,
                description,
                currentUserService.IpAddress,
                currentUserService.UserAgent);

            dbContext.AuditLogs.Add(entry);
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            // Auditing must never break the primary business flow.
            logger.LogError(ex, "Failed to persist audit log entry for action {Action}", action);
        }
    }
}
