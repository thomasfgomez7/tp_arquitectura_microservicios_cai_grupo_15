using Orders.API.Models;

namespace Orders.API.DTOs;

/// <summary>
/// Respuesta de PUT /api/orders/{id}/status: solo el estado nuevo y cuándo cambió.
/// </summary>
public record OrderStatusResponse
{
    /// <summary>ID de la orden.</summary>
    /// <example>f1e2d3c4-0000-0000-0000-aabbccddeeff</example>
    public required Guid Id { get; init; }

    /// <summary>Estado nuevo de la orden.</summary>
    /// <example>Confirmada</example>
    public required string Estado { get; init; }

    /// <summary>Fecha del cambio de estado (UTC), asignada por el servicio.</summary>
    /// <example>2024-03-10T12:00:00Z</example>
    public required DateTime FechaActualizacion { get; init; }

    public static OrderStatusResponse FromEntity(Order order) => new()
    {
        Id = order.Id,
        Estado = order.Estado.ToString(),
        FechaActualizacion = order.FechaActualizacion
    };
}
