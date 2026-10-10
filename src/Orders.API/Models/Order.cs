namespace Orders.API.Models;

/// <summary>
/// Orden de compra (Apéndice A del enunciado).
/// Id, Total, Estado y las fechas los asigna OrderService. FechaActualizacion no está en el Apéndice A,
/// pero la respuesta de PUT /api/orders/{id}/status la incluye (D-33).
/// </summary>
public class Order
{
    public Guid Id { get; set; }

    public Guid UsuarioId { get; set; }

    public List<OrderItem> Items { get; set; } = [];

    public decimal Total { get; set; }

    public OrderStatus Estado { get; set; }

    public DateTime FechaCreacion { get; set; }

    public DateTime FechaActualizacion { get; set; }
}
