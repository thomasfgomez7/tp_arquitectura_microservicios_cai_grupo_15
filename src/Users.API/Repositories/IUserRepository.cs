using Users.API.Models;

namespace Users.API.Repositories;

/// <summary>
/// Persistencia de usuarios. Cuando llegue la librería de la cátedra solo cambia la implementación (D-03).
/// </summary>
public interface IUserRepository
{
    Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken = default);

    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);

    Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task AddAsync(User user, CancellationToken cancellationToken = default);

    Task UpdateAsync(User user, CancellationToken cancellationToken = default);
}