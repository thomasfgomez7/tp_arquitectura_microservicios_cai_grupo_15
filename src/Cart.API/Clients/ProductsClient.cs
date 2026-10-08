using System.Net;

namespace Cart.API.Clients;

/// <summary>
/// Cliente HTTP tipado de Products.API. El HttpClient lo crea IHttpClientFactory con la URL base
/// configurada en "Services:ProductsApi:BaseUrl".
/// </summary>
public class ProductsClient(HttpClient httpClient) : IProductsClient
{
    public async Task<ProductInfo?> GetProductAsync(Guid productId, CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.GetAsync($"api/products/{productId}", cancellationToken);

        // Se revisa el status ANTES de leer el body (sección 10 del enunciado): un 404 trae el JSON de
        // error de Products, no un producto.
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        // Cualquier otro error (500, 503, ...) es una falla de Products, no un dato del negocio:
        // se lanza la excepción y termina en un 500 con CRT-005 (D-28).
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<ProductInfo>(cancellationToken);
    }
}
