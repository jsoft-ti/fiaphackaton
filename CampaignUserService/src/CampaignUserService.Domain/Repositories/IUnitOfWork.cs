namespace CampaignUserService.Domain.Repositories;

/// <summary>
/// Coordinates the persistence of changes made across one or more
/// repositories within a single business transaction.
/// </summary>
public interface IUnitOfWork
{
    IUserRepository Users { get; }

    IRoleRepository Roles { get; }

    IRefreshTokenRepository RefreshTokens { get; }

    IPasswordResetTokenRepository PasswordResetTokens { get; }

    IAuditLogRepository AuditLogs { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);

    Task<IDisposable> BeginTransactionAsync(CancellationToken cancellationToken);
}
