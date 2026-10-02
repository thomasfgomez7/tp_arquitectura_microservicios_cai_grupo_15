namespace Cart.API.Clients;

/// <summary>
/// Consulta a Products.API. El servicio del carrito no sabe que del otro lado hay una llamada HTTP.
/// </summary>
public interface IProductsClient
{
    /// <summary>
    /// Devuelve el producto, o null si Products.API responde 404.
    /// Si Products.API falla o no responde, lanza una excepción (termina en CRT-005, D-28).
    /// </summary>
    Task<ProductInfo?> GetProductAsync(Guid productId, CancellationToken cancellationToken = default);
}
