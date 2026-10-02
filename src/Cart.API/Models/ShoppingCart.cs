namespace Cart.API.Models;

/// <summary>
/// Carrito de un usuario. Se llama ShoppingCart y no Cart porque "Cart" es el namespace raíz del
/// proyecto (Cart.API) y el compilador confundiría ambos. Las operaciones sobre sus items viven acá para que el servicio
/// no tenga que recorrer la lista a mano.
/// </summary>
public class ShoppingCart
{
    public Guid UsuarioId { get; set; }

    public List<CartItem> Items { get; set; } = [];

    public DateTime FechaActualizacion { get; set; }

    public CartItem? FindItem(Guid productoId) =>
        Items.FirstOrDefault(item => item.ProductoId == productoId);

    /// <summary>Cantidad que quedaría del producto si se agregan <paramref name="cantidad"/> unidades (D-13).</summary>
    public int QuantityAfterAdding(Guid productoId, int cantidad) =>
        (FindItem(productoId)?.Cantidad ?? 0) + cantidad;

    /// <summary>Agrega el producto o, si ya estaba, suma la cantidad (D-13).</summary>
    public void AddItem(Guid productoId, int cantidad)
    {
        var item = FindItem(productoId);
        if (item is null)
        {
            Items.Add(new CartItem { ProductoId = productoId, Cantidad = cantidad });
        }
        else
        {
            item.Cantidad += cantidad;
        }
    }

    public void RemoveItem(Guid productoId) =>
        Items.RemoveAll(item => item.ProductoId == productoId);
}
