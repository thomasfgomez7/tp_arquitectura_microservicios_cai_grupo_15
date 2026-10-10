using Orders.API.Models;

namespace Orders.API.Repositories;

/// <summary>
/// Única puerta a la persistencia de órdenes. Cuando llegue la librería de la cátedra
/// solo cambia la implementación (D-03).
/// </summary>
public interface IOrderRepository
{
    /// <summary>
    /// Órdenes de la más vieja a la más nueva. Cada filtro es opcional; si vienen los dos, se aplican juntos.
    /// </summary>
    Task<IReadOnlyList<Order>> GetAllAsync(
        Guid? usuarioId = null,
        Guid? productoId = null,
        CancellationToken cancellationToken = default);

    Task<Order?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task AddAsync(Order order, CancellationToken cancellationToken = default);

    Task UpdateAsync(Order order, CancellationToken cancellationToken = default);
}
