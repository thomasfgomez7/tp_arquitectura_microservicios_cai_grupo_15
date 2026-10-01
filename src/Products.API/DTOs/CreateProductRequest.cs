using System.ComponentModel.DataAnnotations;

namespace Products.API.DTOs;

/// <summary>
/// Datos para crear un producto (POST /api/products).
/// </summary>
public record CreateProductRequest
{
    /// <summary>Nombre del producto. Obligatorio, hasta 100 caracteres; único dentro de la categoría al crear.</summary>
    /// <example>Notebook Dell XPS 15</example>
    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [MaxLength(100, ErrorMessage = "El nombre no puede superar los 100 caracteres.")]
    public string Nombre { get; init; } = string.Empty;

    /// <summary>Descripción opcional, hasta 500 caracteres.</summary>
    /// <example>Laptop 15 pulgadas, 32GB RAM</example>
    [MaxLength(500, ErrorMessage = "La descripción no puede superar los 500 caracteres.")]
    public string? Descripcion { get; init; }

    /// <summary>Precio unitario. Obligatorio, mayor a 0.</summary>
    /// <example>1500.00</example>
    [Required(ErrorMessage = "El precio es obligatorio.")]
    [Range(0, double.MaxValue, MinimumIsExclusive = true, ErrorMessage = "El precio debe ser mayor a 0.")]
    public decimal? Precio { get; init; }

    /// <summary>Unidades disponibles. Obligatorio, mayor o igual a 0.</summary>
    /// <example>10</example>
    [Required(ErrorMessage = "El stock es obligatorio.")]
    [Range(0, int.MaxValue, ErrorMessage = "El stock debe ser mayor o igual a 0.")]
    public int? Stock { get; init; }

    /// <summary>Categoría del producto. Obligatoria; es informativa, no se valida contra una lista.</summary>
    /// <example>Electrónica</example>
    [Required(ErrorMessage = "La categoría es obligatoria.")]
    public string Categoria { get; init; } = string.Empty;
}
