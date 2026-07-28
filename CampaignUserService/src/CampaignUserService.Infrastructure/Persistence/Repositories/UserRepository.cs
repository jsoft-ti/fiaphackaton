using CampaignUserService.Domain.Entities;
using CampaignUserService.Domain.Enums;
using CampaignUserService.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace CampaignUserService.Infrastructure.Persistence.Repositories;

public sealed class UserRepository(ApplicationDbContext dbContext) : IUserRepository
{
    public async Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken, bool includeRoles = true)
    {
        var query = dbContext.Users.AsQueryable();

        if (includeRoles)
        {
            query = query.Include(u => u.UserRoles).ThenInclude(ur => ur.Role);
        }

        return await query.FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
    }

    public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken, bool includeRoles = true)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();
        var query = dbContext.Users.AsQueryable();

        if (includeRoles)
        {
            query = query.Include(u => u.UserRoles).ThenInclude(ur => ur.Role);
        }

        return await query.FirstOrDefaultAsync(u => u.Email == normalizedEmail, cancellationToken);
    }

    public Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();
        return dbContext.Users.AnyAsync(u => u.Email == normalizedEmail, cancellationToken);
    }

    public Task<bool> ExistsByCpfAsync(string cpf, CancellationToken cancellationToken) =>
        dbContext.Users.AnyAsync(u => u.Cpf == cpf, cancellationToken);

    public async Task<(IReadOnlyList<User> Items, int TotalCount)> SearchAsync(
        string? searchTerm,
        RoleName? role,
        UserStatus? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var query = dbContext.Users
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.Trim().ToLowerInvariant();
            query = query.Where(u =>
                EF.Functions.ILike(u.FirstName, $"%{term}%") ||
                EF.Functions.ILike(u.LastName, $"%{term}%") ||
                EF.Functions.ILike(u.Email, $"%{term}%"));
        }

        if (role.HasValue)
        {
            query = query.Where(u => u.UserRoles.Any(ur => ur.Role!.Name == role.Value));
        }

        if (status.HasValue)
        {
            query = query.Where(u => u.Status == status.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(u => u.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public void Add(User user) => dbContext.Users.Add(user);

    public void Remove(User user) => dbContext.Users.Remove(user);
}
