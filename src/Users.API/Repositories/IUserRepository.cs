using Users.API.Models;

namespace Users.API.Repositories;

public interface IUserRepository
{
    Task<bool> ExisteEmailAsync(string email, CancellationToken cancellationToken = default);
    Task AgregarAsync(User user, CancellationToken cancellationToken = default);
    Task<User?> ObtenerPorEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<User?> ObtenerPorIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task ActualizarAsync(User user, CancellationToken cancellationToken = default);
}