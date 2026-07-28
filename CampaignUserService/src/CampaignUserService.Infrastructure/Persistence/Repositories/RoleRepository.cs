using CampaignUserService.Domain.Entities;
using CampaignUserService.Domain.Enums;
using CampaignUserService.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace CampaignUserService.Infrastructure.Persistence.Repositories;

public sealed class RoleRepository(ApplicationDbContext dbContext) : IRoleRepository
{
    public Task<Role?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        dbContext.Roles.FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

    public Task<Role?> GetByNameAsync(RoleName name, CancellationToken cancellationToken) =>
        dbContext.Roles.FirstOrDefaultAsync(r => r.Name == name, cancellationToken);

    public async Task<IReadOnlyList<Role>> GetAllAsync(CancellationToken cancellationToken) =>
        await dbContext.Roles.OrderBy(r => r.Name).ToListAsync(cancellationToken);

    public Task<bool> ExistsByNameAsync(RoleName name, CancellationToken cancellationToken) =>
        dbContext.Roles.AnyAsync(r => r.Name == name, cancellationToken);

    public void Add(Role role) => dbContext.Roles.Add(role);
}
