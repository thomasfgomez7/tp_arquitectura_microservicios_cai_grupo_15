using System.Collections.Concurrent;
using Products.API.Models;

namespace Products.API.Repositories;

/// <summary>
/// Persistencia en memoria. La app usa <see cref="SqliteProductRepository"/> (D-40); esta implementación
/// queda como "fake" de los tests unitarios del servicio: guarda de verdad, sin disco.
/// </summary>
public class InMemoryProductRepository : IProductRepository
{
    private readonly ConcurrentDictionary<Guid, Product> _products;

    public InMemoryProductRepository(IEnumerable<Product>? initialProducts = null)
    {
        _products = new ConcurrentDictionary<Guid, Product>(
            (initialProducts ?? []).ToDictionary(p => p.Id));
    }

    public Task<IReadOnlyList<Product>> GetAllAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<Product>>(_products.Values.ToList());

    public Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_products.GetValueOrDefault(id));

    public Task AddAsync(Product product, CancellationToken cancellationToken = default)
    {
        _products[product.Id] = product;
        return Task.CompletedTask;
    }

    public Task UpdateAsync(Product product, CancellationToken cancellationToken = default)
    {
        _products[product.Id] = product;
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _products.TryRemove(id, out _);
        return Task.CompletedTask;
    }
}
