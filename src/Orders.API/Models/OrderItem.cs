namespace Orders.API.Models;

/// <summary>
/// Producto de una orden (Apéndice A del enunciado). PrecioUnitario se copia del producto al crear la orden,
/// así un cambio de precio posterior en Products.API no altera órdenes ya hechas.
/// </summary>
public class OrderItem
{
    public Guid ProductoId { get; set; }

    public int Cantidad { get; set; }

    public decimal PrecioUnitario { get; set; }
}
