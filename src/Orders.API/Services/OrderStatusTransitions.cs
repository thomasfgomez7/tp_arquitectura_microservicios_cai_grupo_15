using Orders.API.Models;

namespace Orders.API.Services;

/// <summary>
/// Máquina de estados de la orden: Pendiente → Confirmada → Enviada → Entregada,
/// y Pendiente o Confirmada → Cancelada. Entregada y Cancelada son finales.
/// Cualquier otro cambio (incluido quedar en el mismo estado) es inválido y da ORD-006.
/// </summary>
public static class OrderStatusTransitions
{
    private static readonly Dictionary<OrderStatus, OrderStatus[]> AllowedTransitions = new()
    {
        [OrderStatus.Pendiente] = [OrderStatus.Confirmada, OrderStatus.Cancelada],
        [OrderStatus.Confirmada] = [OrderStatus.Enviada, OrderStatus.Cancelada],
        [OrderStatus.Enviada] = [OrderStatus.Entregada],
        [OrderStatus.Entregada] = [],
        [OrderStatus.Cancelada] = []
    };

    public static bool CanTransition(OrderStatus from, OrderStatus to) =>
        AllowedTransitions[from].Contains(to);
}
