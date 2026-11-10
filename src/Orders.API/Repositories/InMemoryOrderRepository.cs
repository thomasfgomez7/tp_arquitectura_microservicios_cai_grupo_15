using System.Collections.Concurrent;
using Orders.API.Models;

namespace Orders.API.Repositories;

/// <summary>
/// Persistencia en memoria. La app usa <see cref="SqliteOrderRepository"/> (D-40); esta implementación
/// queda como "fake" de los tests unitarios del servicio: guarda de verdad, sin disco.
/// </summary>
public class InMemoryOrderRepository : IOrderRepository
{
    private readonly ConcurrentDictionary<Guid, Order> _orders;

    public InMemoryOrderRepository(IEnumerable<Order>? initialOrders = null)
    {
        _orders = new ConcurrentDictionary<Guid, Order>(
            (initialOrders ?? []).ToDictionary(order => order.Id));
    }

    public Task<IReadOnlyList<Order>> GetAllAsync(
        Guid? usuarioId = null,
        Guid? productoId = null,
        CancellationToken cancellationToken = default)
    {
        // Lista ya materializada y ordenada de la más vieja a la más nueva.
        IReadOnlyList<Order> result = _orders.Values
            .Where(order => usuarioId is null || order.UsuarioId == usuarioId)
            .Where(order => productoId is null || order.Items.Any(item => item.ProductoId == productoId))
            .OrderBy(order => order.FechaCreacion)
            .ToList();

        return Task.FromResult(result);
    }

    public Task<Order?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_orders.GetValueOrDefault(id));

    public Task AddAsync(Order order, CancellationToken cancellationToken = default)
    {
        _orders[order.Id] = order;
        return Task.CompletedTask;
    }

    public Task UpdateAsync(Order order, CancellationToken cancellationToken = default)
    {
        _orders[order.Id] = order;
        return Task.CompletedTask;
    }
}
