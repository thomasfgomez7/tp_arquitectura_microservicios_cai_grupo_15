using System.Net;
using System.Net.Http.Json;
using Cart.API.Exceptions;

namespace Cart.API.Tests.Integration;

/// <summary>
/// Cart.API colaborando con Products.API (Etapa 9): Products caído con el ProductsClient real
/// y propagación del Correlation ID. Cada test usa su propio factory porque modifica el Products falso.
/// </summary>
public class ProductsIntegrationTests : IDisposable
{
    private const string Header = "X-Correlation-Id";

    private readonly CartApiFactory _factory = new();
    private readonly HttpClient _client;

    public ProductsIntegrationTests() => _client = _factory.CreateClient();

    [Fact]
    public async Task AddItem_ProductsCaido_Devuelve500ConCRT005()
    {
        // D-28: una falla de Products.API es un error de infraestructura.
        _factory.ProductsApi.Caido = true;
        var usuario = Guid.NewGuid();

        var response = await _client.PostAsJsonAsync($"/api/cart/{usuario}/items", Agregar(FakeProductsApi.Notebook));

        await ErrorContractAssert.IsErrorAsync(response, HttpStatusCode.InternalServerError, ErrorCodes.CRT_005,
            instance: $"/api/cart/{usuario}/items", errorMessage: "Error interno al procesar el carrito.");
    }

    [Fact]
    public async Task AddItem_PropagaElCorrelationIdDelRequestALaLlamadaAProducts()
    {
        var request = new HttpRequestMessage(HttpMethod.Post, $"/api/cart/{Guid.NewGuid()}/items")
        {
            Content = JsonContent.Create(Agregar(FakeProductsApi.Notebook))
        };
        request.Headers.Add(Header, "demo-etapa-9");

        await _client.SendAsync(request);

        Assert.Equal("demo-etapa-9", Assert.Single(_factory.ProductsApi.CorrelationIdsRecibidos));
    }

    [Fact]
    public async Task AddItem_SinHeader_PropagaElCorrelationIdGenerado()
    {
        var response = await _client.PostAsJsonAsync($"/api/cart/{Guid.NewGuid()}/items", Agregar(FakeProductsApi.Notebook));

        var generado = response.Headers.GetValues(Header).Single();
        Assert.Equal(generado, Assert.Single(_factory.ProductsApi.CorrelationIdsRecibidos));
    }

    private static object Agregar(Guid productoId) => new { productoId, cantidad = 1 };

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
    }
}
