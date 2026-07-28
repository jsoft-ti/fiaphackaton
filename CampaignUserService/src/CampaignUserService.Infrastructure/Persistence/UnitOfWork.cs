using CampaignUserService.Domain.Repositories;
using CampaignUserService.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore.Storage;

namespace CampaignUserService.Infrastructure.Persistence;

public sealed class UnitOfWork : IUnitOfWork
{
    private readonly ApplicationDbContext _dbContext;

    public UnitOfWork(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
        Users = new UserRepository(dbContext);
        Roles = new RoleRepository(dbContext);
        RefreshTokens = new RefreshTokenRepository(dbContext);
        PasswordResetTokens = new PasswordResetTokenRepository(dbContext);
        AuditLogs = new AuditLogRepository(dbContext);
    }

    public IUserRepository Users { get; }

    public IRoleRepository Roles { get; }

    public IRefreshTokenRepository RefreshTokens { get; }

    public IPasswordResetTokenRepository PasswordResetTokens { get; }

    public IAuditLogRepository AuditLogs { get; }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken) =>
        _dbContext.SaveChangesAsync(cancellationToken);

    public async Task<IDisposable> BeginTransactionAsync(CancellationToken cancellationToken)
    {
        IDbContextTransaction transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
        return transaction;
    }
}
