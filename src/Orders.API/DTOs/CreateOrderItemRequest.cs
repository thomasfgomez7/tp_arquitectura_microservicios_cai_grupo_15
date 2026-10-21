using System.ComponentModel.DataAnnotations;

namespace Orders.API.DTOs;

/// <summary>
/// Un producto de la orden que se quiere crear. El precio no se envía: se toma de Products.API.
/// </summary>
public record CreateOrderItemRequest
{
    /// <summary>ID del producto en Products.API. Obligatorio.</summary>
    /// <example>3fa85f64-5717-4562-b3fc-2c963f66afa6</example>
    [Required(ErrorMessage = "El producto es obligatorio.")]
    public Guid? ProductoId { get; init; }

    /// <summary>Unidades a comprar. Obligatoria, mayor a cero.</summary>
    /// <example>2</example>
    [Required(ErrorMessage = "La cantidad es obligatoria.")]
    [Range(1, int.MaxValue, ErrorMessage = "La cantidad debe ser mayor a cero.")]
    public int? Cantidad { get; init; }
}
