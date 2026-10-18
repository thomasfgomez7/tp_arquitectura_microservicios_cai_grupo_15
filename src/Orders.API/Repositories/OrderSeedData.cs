using Orders.API.Models;

namespace Orders.API.Repositories;

/// <summary>
/// Orden precargada para la demo: la del ejemplo del enunciado (María compra dos notebooks de los datos
/// semilla de Users.API y Products.API). Está Pendiente, así que es una orden activa: con ella,
/// DELETE /api/products/{notebook} responde PRD-004 apenas se levantan los servicios.
/// </summary>
public static class OrderSeedData
{
    public static IReadOnlyList<Order> Create() =>
    [
        new()
        {
            Id = Guid.Parse("f1e2d3c4-0000-0000-0000-aabbccddeeff"),
            UsuarioId = Guid.Parse("a1b2c3d4-0000-0000-0000-111122223333"),
            Items =
            [
                new OrderItem
                {
                    ProductoId = Guid.Parse("3fa85f64-5717-4562-b3fc-2c963f66afa6"),
                    Cantidad = 2,
                    PrecioUnitario = 1500.00m
                }
            ],
            Total = 3000.00m,
            Estado = OrderStatus.Pendiente,
            FechaCreacion = new DateTime(2024, 3, 10, 11, 0, 0, DateTimeKind.Utc),
            FechaActualizacion = new DateTime(2024, 3, 10, 11, 0, 0, DateTimeKind.Utc)
        }
    ];
}
