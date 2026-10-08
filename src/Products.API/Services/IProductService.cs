using Products.API.DTOs;

namespace Products.API.Services;

/// <summary>
/// Reglas de negocio de productos (PRD-001 a PRD-004).
/// </summary>
public interface IProductService
{
    /// <summary>
    /// Lista los productos. <paramref name="categoria"/> filtra por coincidencia exacta y
    /// <paramref name="nombre"/> por coincidencia parcial; ambos sin distinguir mayúsculas.
    /// </summary>
    Task<IReadOnlyList<ProductResponse>> GetAllAsync(string? categoria, string? nombre, CancellationToken cancellationToken = default);

    Task<ProductResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<ProductResponse> CreateAsync(CreateProductRequest request, CancellationToken cancellationToken = default);

    Task<ProductResponse> UpdateAsync(Guid id, UpdateProductRequest request, CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
