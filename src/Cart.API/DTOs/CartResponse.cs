using Cart.API.Models;

namespace Cart.API.DTOs;

/// <summary>
/// Carrito tal como lo devuelve la API.
/// </summary>
public record CartResponse
{
    /// <summary>ID del usuario dueño del carrito.</summary>
    /// <example>a1b2c3d4-0000-0000-0000-111122223333</example>
    public required Guid UsuarioId { get; init; }

    /// <summary>Productos del carrito.</summary>
    public required IReadOnlyList<CartItemResponse> Items { get; init; }

    /// <summary>Fecha de la última operación sobre el carrito (UTC).</summary>
    /// <example>2024-03-10T10:45:00Z</example>
    public required DateTime FechaActualizacion { get; init; }

    public static CartResponse FromEntity(ShoppingCart cart) => new()
    {
        UsuarioId = cart.UsuarioId,
        Items = cart.Items
            .Select(item => new CartItemResponse { ProductoId = item.ProductoId, Cantidad = item.Cantidad })
            .ToList(),
        FechaActualizacion = cart.FechaActualizacion
    };
}
