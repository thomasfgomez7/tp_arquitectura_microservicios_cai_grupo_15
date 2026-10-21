using Orders.API.Clients;
using Orders.API.DTOs;
using Orders.API.Exceptions;
using Orders.API.Models;
using Orders.API.Repositories;

namespace Orders.API.Services;

public class OrderService(
    IOrderRepository repository,
    IUsersClient usersClient,
    IProductsClient productsClient,
    TimeProvider timeProvider) : IOrderService
{
    public async Task<IReadOnlyList<OrderResponse>> GetAllAsync(
        Guid? usuarioId = null,
        Guid? productoId = null,
        CancellationToken cancellationToken = default)
    {
        var orders = await repository.GetAllAsync(usuarioId, productoId, cancellationToken);

        return orders.Select(OrderResponse.FromEntity).ToList();
    }

    public async Task<OrderResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var order = await GetExistingOrderAsync(id, cancellationToken);

        return OrderResponse.FromEntity(order);
    }

    public async Task<OrderResponse> CreateAsync(CreateOrderRequest request, CancellationToken cancellationToken = default)
    {
        // Las Data Annotations ya lo validan antes de llegar acá; esto protege al servicio
        // si lo usa otro código.
        var usuarioId = request.UsuarioId
            ?? throw new ValidationException(ErrorCodes.ORD_002, "El usuario es obligatorio.");

        if (request.Items is null || request.Items.Count == 0)
        {
            throw new ValidationException(ErrorCodes.ORD_002, "La orden debe tener al menos un item.");
        }

        if (request.Items.Any(item => item.ProductoId is null || item.Cantidad is null or <= 0))
        {
            throw new ValidationException(ErrorCodes.ORD_002, "Los items de la orden son inválidos.");
        }

        // D-35: un usuario bloqueado puede comprar; solo se verifica que exista.
        if (await usersClient.GetUserAsync(usuarioId, cancellationToken) is null)
        {
            throw new NotFoundException(ErrorCodes.ORD_003, "Usuario no encontrado al crear la orden.");
        }

        // D-34: si un producto aparece en varios items, se unen en uno y el stock se valida contra el total.
        var cantidadesPorProducto = request.Items
            .GroupBy(item => item.ProductoId!.Value)
            .Select(group => (ProductoId: group.Key, Cantidad: group.Sum(item => item.Cantidad!.Value)));

        var items = new List<OrderItem>();
        foreach (var (productoId, cantidad) in cantidadesPorProducto)
        {
            var product = await GetProductWithStockAsync(productoId, cantidad, cancellationToken);

            // El precio se toma de Products.API, no del request.
            items.Add(new OrderItem { ProductoId = productoId, Cantidad = cantidad, PrecioUnitario = product.Precio });
        }

        var ahora = timeProvider.GetUtcNow().UtcDateTime;
        var order = new Order
        {
            Id = Guid.NewGuid(),
            UsuarioId = usuarioId,
            Items = items,
            Total = items.Sum(item => item.Cantidad * item.PrecioUnitario),
            Estado = OrderStatus.Pendiente,
            FechaCreacion = ahora,
            FechaActualizacion = ahora
        };

        // D-14: crear la orden no descuenta stock en Products.API.
        await repository.AddAsync(order, cancellationToken);

        return OrderResponse.FromEntity(order);
    }

    public async Task<OrderStatusResponse> UpdateStatusAsync(
        Guid id,
        UpdateOrderStatusRequest request,
        CancellationToken cancellationToken = default)
    {
        // Igual que en CreateAsync: las Data Annotations ya lo validan, esto protege al servicio.
        if (!Enum.GetNames<OrderStatus>().Contains(request.Estado))
        {
            throw new ValidationException(
                ErrorCodes.ORD_002,
                "El estado debe ser Pendiente, Confirmada, Enviada, Entregada o Cancelada.");
        }

        var nuevoEstado = Enum.Parse<OrderStatus>(request.Estado);
        var order = await GetExistingOrderAsync(id, cancellationToken);

        if (!OrderStatusTransitions.CanTransition(order.Estado, nuevoEstado))
        {
            throw new BusinessRuleException(
                ErrorCodes.ORD_006,
                $"Una orden en estado '{order.Estado}' no puede pasar a '{nuevoEstado}'.",
                StatusCodes.Status409Conflict);
        }

        order.Estado = nuevoEstado;
        order.FechaActualizacion = timeProvider.GetUtcNow().UtcDateTime;
        await repository.UpdateAsync(order, cancellationToken);

        return OrderStatusResponse.FromEntity(order);
    }

    private async Task<Order> GetExistingOrderAsync(Guid id, CancellationToken cancellationToken) =>
        await repository.GetByIdAsync(id, cancellationToken)
        ?? throw new NotFoundException(ErrorCodes.ORD_001, "Orden no encontrada.");

    /// <summary>
    /// Consulta el producto en Products.API: ORD-004 si no existe, ORD-005 si no alcanza el stock.
    /// </summary>
    private async Task<ProductInfo> GetProductWithStockAsync(
        Guid productoId,
        int cantidadSolicitada,
        CancellationToken cancellationToken)
    {
        var product = await productsClient.GetProductAsync(productoId, cancellationToken)
                      ?? throw new NotFoundException(ErrorCodes.ORD_004, "Producto no encontrado al crear la orden.");

        if (cantidadSolicitada > product.Stock)
        {
            throw new BusinessRuleException(
                ErrorCodes.ORD_005,
                $"Stock insuficiente para '{product.Nombre}'. Disponible: {product.Stock}, solicitado: {cantidadSolicitada}.",
                StatusCodes.Status422UnprocessableEntity);
        }

        return product;
    }
}
