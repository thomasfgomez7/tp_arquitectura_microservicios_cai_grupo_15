using System.Collections.Concurrent;
using Cart.API.Models;

namespace Cart.API.Repositories;

/// <summary>
/// Persistencia en memoria. La app usa <see cref="SqliteCartRepository"/> (D-40); esta implementación
/// queda como "fake" de los tests unitarios del servicio: guarda de verdad, sin disco.
/// </summary>
public class InMemoryCartRepository : ICartRepository
{
    private readonly ConcurrentDictionary<Guid, ShoppingCart> _carts;

    public InMemoryCartRepository(IEnumerable<ShoppingCart>? initialCarts = null)
    {
        _carts = new ConcurrentDictionary<Guid, ShoppingCart>(
            (initialCarts ?? []).ToDictionary(cart => cart.UsuarioId));
    }

    public Task<ShoppingCart?> GetByUserIdAsync(Guid usuarioId, CancellationToken cancellationToken = default) =>
        Task.FromResult(_carts.GetValueOrDefault(usuarioId));

    public Task SaveAsync(ShoppingCart cart, CancellationToken cancellationToken = default)
    {
        _carts[cart.UsuarioId] = cart;
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Guid usuarioId, CancellationToken cancellationToken = default)
    {
        _carts.TryRemove(usuarioId, out _);
        return Task.CompletedTask;
    }
}
