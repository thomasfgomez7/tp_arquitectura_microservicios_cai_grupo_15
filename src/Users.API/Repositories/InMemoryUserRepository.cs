using System.Collections.Concurrent;
using Users.API.Models;

namespace Users.API.Repositories;

/// <summary>
/// Persistencia en memoria hasta recibir la librería de la cátedra (D-03).
/// Se registra como Singleton para que los datos sobrevivan entre requests.
/// </summary>
public class InMemoryUserRepository : IUserRepository
{
    // La clave es el email sin distinguir mayúsculas: el email queda único
    // aunque lleguen dos registros iguales al mismo tiempo.
    private readonly ConcurrentDictionary<string, User> _usersByEmail;

    public InMemoryUserRepository(IEnumerable<User>? initialUsers = null)
    {
        _usersByEmail = new ConcurrentDictionary<string, User>(
            (initialUsers ?? []).ToDictionary(user => user.Email, StringComparer.OrdinalIgnoreCase),
            StringComparer.OrdinalIgnoreCase);
    }

    public Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken = default) =>
        Task.FromResult(_usersByEmail.ContainsKey(email));

    public Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default) =>
        Task.FromResult(_usersByEmail.GetValueOrDefault(email));

    public Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_usersByEmail.Values.FirstOrDefault(user => user.Id == id));

    public Task AddAsync(User user, CancellationToken cancellationToken = default)
    {
        // UserService ya verificó el email antes. Esto solo pasa si dos registros iguales
        // llegan al mismo tiempo: termina en un 500 (USR-006) en lugar de pisar al usuario.
        if (!_usersByEmail.TryAdd(user.Email, user))
        {
            throw new InvalidOperationException($"Ya existe un usuario con el email '{user.Email}'.");
        }

        return Task.CompletedTask;
    }

    public Task UpdateAsync(User user, CancellationToken cancellationToken = default)
    {
        _usersByEmail[user.Email] = user;
        return Task.CompletedTask;
    }
}