using System.ComponentModel.DataAnnotations;

namespace Cart.API.DTOs;

/// <summary>
/// Datos para cambiar la cantidad de un producto del carrito (PUT /api/cart/{userId}/items/{productId}).
/// </summary>
public record UpdateCartItemRequest
{
    /// <summary>Cantidad nueva (reemplaza a la anterior). Obligatoria, mayor a cero.</summary>
    /// <example>4</example>
    [Required(ErrorMessage = "La cantidad es obligatoria.")]
    [Range(1, int.MaxValue, ErrorMessage = "La cantidad debe ser mayor a cero.")]
    public int? Cantidad { get; init; }
}
