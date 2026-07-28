using CampaignUserService.Domain.Entities;
using CampaignUserService.Domain.Enums;

namespace CampaignUserService.Domain.Repositories;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken, bool includeRoles = true);

    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken, bool includeRoles = true);

    Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken);

    Task<bool> ExistsByCpfAsync(string cpf, CancellationToken cancellationToken);

    Task<(IReadOnlyList<User> Items, int TotalCount)> SearchAsync(
        string? searchTerm,
        RoleName? role,
        UserStatus? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    void Add(User user);

    void Remove(User user);
}
