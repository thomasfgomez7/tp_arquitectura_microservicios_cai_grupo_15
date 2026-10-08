using System.ComponentModel.DataAnnotations;

namespace Cart.API.DTOs;

/// <summary>
/// Datos para agregar un producto al carrito (POST /api/cart/{userId}/items).
/// </summary>
public record AddCartItemRequest
{
    /// <summary>ID del producto en Products.API. Obligatorio.</summary>
    /// <example>3fa85f64-5717-4562-b3fc-2c963f66afa6</example>
    [Required(ErrorMessage = "El producto es obligatorio.")]
    public Guid? ProductoId { get; init; }

    /// <summary>Unidades a agregar. Obligatoria, mayor a cero. Si el producto ya está en el carrito, se suma.</summary>
    /// <example>2</example>
    [Required(ErrorMessage = "La cantidad es obligatoria.")]
    [Range(1, int.MaxValue, ErrorMessage = "La cantidad debe ser mayor a cero.")]
    public int? Cantidad { get; init; }
}
