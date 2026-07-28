using CampaignUserService.Domain.Entities;
using CampaignUserService.Domain.Enums;

namespace CampaignUserService.Domain.Repositories;

public interface IRoleRepository
{
    Task<Role?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<Role?> GetByNameAsync(RoleName name, CancellationToken cancellationToken);

    Task<IReadOnlyList<Role>> GetAllAsync(CancellationToken cancellationToken);

    Task<bool> ExistsByNameAsync(RoleName name, CancellationToken cancellationToken);

    void Add(Role role);
}
