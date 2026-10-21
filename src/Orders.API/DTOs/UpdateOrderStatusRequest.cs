using System.ComponentModel.DataAnnotations;

namespace Orders.API.DTOs;

/// <summary>
/// Datos para cambiar el estado de una orden (PUT /api/orders/{id}/status).
/// </summary>
public record UpdateOrderStatusRequest
{
    /// <summary>Estado nuevo: Pendiente, Confirmada, Enviada, Entregada o Cancelada. Uno desconocido da ORD-002.</summary>
    /// <example>Confirmada</example>
    [Required(ErrorMessage = "El estado es obligatorio.")]
    [RegularExpression(
        "^(Pendiente|Confirmada|Enviada|Entregada|Cancelada)$",
        ErrorMessage = "El estado debe ser Pendiente, Confirmada, Enviada, Entregada o Cancelada.")]
    public string Estado { get; init; } = string.Empty;
}
