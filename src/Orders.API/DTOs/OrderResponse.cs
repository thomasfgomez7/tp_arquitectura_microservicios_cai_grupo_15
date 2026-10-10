using Orders.API.Models;

namespace Orders.API.DTOs;

/// <summary>
/// Orden tal como la devuelve la API.
/// </summary>
public record OrderResponse
{
    /// <summary>ID de la orden.</summary>
    /// <example>f1e2d3c4-0000-0000-0000-aabbccddeeff</example>
    public required Guid Id { get; init; }

    /// <summary>ID del usuario que hizo la orden.</summary>
    /// <example>a1b2c3d4-0000-0000-0000-111122223333</example>
    public required Guid UsuarioId { get; init; }

    /// <summary>Productos de la orden.</summary>
    public required IReadOnlyList<OrderItemResponse> Items { get; init; }

    /// <summary>Suma de cantidad × precio unitario de cada item.</summary>
    /// <example>3000.00</example>
    public required decimal Total { get; init; }

    /// <summary>Pendiente, Confirmada, Enviada, Entregada o Cancelada.</summary>
    /// <example>Pendiente</example>
    public required string Estado { get; init; }

    /// <summary>Fecha de creación (UTC), asignada por el servicio.</summary>
    /// <example>2024-03-10T11:00:00Z</example>
    public required DateTime FechaCreacion { get; init; }

    // El enum sale como texto ("Pendiente"), igual que en el contrato del enunciado.
    public static OrderResponse FromEntity(Order order) => new()
    {
        Id = order.Id,
        UsuarioId = order.UsuarioId,
        Items = order.Items
            .Select(item => new OrderItemResponse
            {
                ProductoId = item.ProductoId,
                Cantidad = item.Cantidad,
                PrecioUnitario = item.PrecioUnitario
            })
            .ToList(),
        Total = order.Total,
        Estado = order.Estado.ToString(),
        FechaCreacion = order.FechaCreacion
    };
}
