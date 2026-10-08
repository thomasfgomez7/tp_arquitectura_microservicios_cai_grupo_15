using Cart.API.Models;

namespace Cart.API.Repositories;

/// <summary>
/// Única puerta a la persistencia de carritos. Cuando llegue la librería de la cátedra
/// solo cambia la implementación (D-03).
/// </summary>
public interface ICartRepository
{
    Task<ShoppingCart?> GetByUserIdAsync(Guid usuarioId, CancellationToken cancellationToken = default);

    /// <summary>Crea el carrito si no existe o lo reemplaza si ya existía.</summary>
    Task SaveAsync(ShoppingCart cart, CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid usuarioId, CancellationToken cancellationToken = default);
}
