namespace Products.API.Clients;

/// <summary>
/// Cliente HTTP tipado de Orders.API. El HttpClient lo crea IHttpClientFactory con la URL base
/// configurada en "Services:OrdersApi:BaseUrl", el timeout y el CorrelationIdDelegatingHandler.
/// </summary>
public class OrdersClient(HttpClient httpClient) : IOrdersClient
{
    public async Task<bool> HasActiveOrdersAsync(Guid productId, CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.GetAsync($"api/orders?productoId={productId}", cancellationToken);

        // Un producto sin órdenes responde 200 con [] (D-37), así que cualquier error es una falla de Orders:
        // se lanza la excepción y termina en un 500 con PRD-005, sin borrar el producto (D-39).
        response.EnsureSuccessStatusCode();

        var orders = await response.Content.ReadFromJsonAsync<List<OrderInfo>>(cancellationToken) ?? [];

        return orders.Any(order => order.EstaActiva);
    }
}
