using System.ComponentModel.DataAnnotations;

namespace Orders.API.DTOs;

/// <summary>
/// Datos para crear una orden (POST /api/orders).
/// </summary>
public record CreateOrderRequest
{
    /// <summary>ID del usuario en Users.API. Obligatorio. Es Guid? para que [Required] detecte si falta.</summary>
    /// <example>a1b2c3d4-0000-0000-0000-111122223333</example>
    [Required(ErrorMessage = "El usuario es obligatorio.")]
    public Guid? UsuarioId { get; init; }

    /// <summary>Productos de la orden. Obligatoria, con al menos un item (ORD-002).</summary>
    [Required(ErrorMessage = "Los items son obligatorios.")]
    [MinLength(1, ErrorMessage = "La orden debe tener al menos un item.")]
    public IReadOnlyList<CreateOrderItemRequest> Items { get; init; } = [];
}
