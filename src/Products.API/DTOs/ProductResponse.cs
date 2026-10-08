using Products.API.Models;

namespace Products.API.DTOs;

/// <summary>
/// Producto tal como lo devuelve la API.
/// </summary>
public record ProductResponse
{
    /// <summary>Identificador único del producto.</summary>
    /// <example>3fa85f64-5717-4562-b3fc-2c963f66afa6</example>
    public required Guid Id { get; init; }

    /// <summary>Nombre del producto.</summary>
    /// <example>Notebook Dell XPS 15</example>
    public required string Nombre { get; init; }

    /// <summary>Descripción del producto (opcional).</summary>
    /// <example>Laptop 15 pulgadas, 32GB RAM</example>
    public string? Descripcion { get; init; }

    /// <summary>Precio unitario.</summary>
    /// <example>1500.00</example>
    public required decimal Precio { get; init; }

    /// <summary>Unidades disponibles.</summary>
    /// <example>10</example>
    public required int Stock { get; init; }

    /// <summary>Categoría del producto.</summary>
    /// <example>Electrónica</example>
    public required string Categoria { get; init; }

    /// <summary>Fecha de creación (UTC), asignada por el servicio.</summary>
    /// <example>2024-01-15T10:30:00Z</example>
    public required DateTime FechaCreacion { get; init; }

    public static ProductResponse FromEntity(Product product) => new()
    {
        Id = product.Id,
        Nombre = product.Nombre,
        Descripcion = product.Descripcion,
        Precio = product.Precio,
        Stock = product.Stock,
        Categoria = product.Categoria,
        FechaCreacion = product.FechaCreacion
    };
}
