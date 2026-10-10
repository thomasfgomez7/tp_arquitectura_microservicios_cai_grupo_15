using Orders.API.DTOs;

namespace Orders.API.Services;

/// <summary>
/// Reglas de las órdenes (ORD-001 a ORD-006).
/// </summary>
public interface IOrderService
{
    /// <summary>Lista las órdenes, filtrando por usuario y/o por producto (D-07).</summary>
    Task<IReadOnlyList<OrderResponse>> GetAllAsync(
        Guid? usuarioId = null,
        Guid? productoId = null,
        CancellationToken cancellationToken = default);

    Task<OrderResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Crea la orden en estado Pendiente, validando usuario, productos y stock contra los otros servicios.</summary>
    Task<OrderResponse> CreateAsync(CreateOrderRequest request, CancellationToken cancellationToken = default);

    /// <summary>Cambia el estado si la transición está permitida (OrderStatusTransitions).</summary>
    Task<OrderStatusResponse> UpdateStatusAsync(
        Guid id,
        UpdateOrderStatusRequest request,
        CancellationToken cancellationToken = default);
}
