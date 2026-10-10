namespace Orders.API.Models;

/// <summary>
/// Estado de la orden (Apéndice A del enunciado). Los cambios permitidos están en OrderStatusTransitions.
/// </summary>
public enum OrderStatus
{
    Pendiente,
    Confirmada,
    Enviada,
    Entregada,
    Cancelada
}
