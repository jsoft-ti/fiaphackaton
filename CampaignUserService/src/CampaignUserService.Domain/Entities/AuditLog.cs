using CampaignUserService.Domain.Enums;
using CampaignUserService.SharedKernel.Common;

namespace CampaignUserService.Domain.Entities;

/// <summary>
/// Immutable audit trail entry. Written for every security-sensitive
/// operation (login, logout, registration, password change, role change,
/// user CRUD) as required by the security/compliance requirements.
/// </summary>
public class AuditLog : BaseEntity
{
    protected AuditLog()
    {
    }

    private AuditLog(
        Guid? userId,
        AuditActionType action,
        string description,
        string? ipAddress,
        string? userAgent)
    {
        UserId = userId;
        Action = action;
        Description = description;
        IpAddress = ipAddress;
        UserAgent = userAgent;
        OccurredAtUtc = DateTime.UtcNow;
    }

    public Guid? UserId { get; private set; }

    public User? User { get; private set; }

    public AuditActionType Action { get; private set; }

    public string Description { get; private set; } = string.Empty;

    public string? IpAddress { get; private set; }

    public string? UserAgent { get; private set; }

    public DateTime OccurredAtUtc { get; private set; }

    public static AuditLog Create(
        Guid? userId,
        AuditActionType action,
        string description,
        string? ipAddress,
        string? userAgent) =>
        new(userId, action, description, ipAddress, userAgent);
}
