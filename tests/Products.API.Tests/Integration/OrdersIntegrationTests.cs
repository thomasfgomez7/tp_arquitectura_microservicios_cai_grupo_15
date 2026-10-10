using System.Net;
using System.Net.Http.Json;
using Products.API.Exceptions;

namespace Products.API.Tests.Integration;

/// <summary>
/// Products.API colaborando con Orders.API (Etapa 9): PRD-004 con el OrdersClient real
/// y propagación del Correlation ID. Cada test usa su propio factory porque modifica el Orders falso.
/// </summary>
public class OrdersIntegrationTests : IDisposable
{
    private const string Header = "X-Correlation-Id";

    private readonly ProductsApiFactory _factory = new();
    private readonly HttpClient _client;

    public OrdersIntegrationTests() => _client = _factory.CreateClient();

    [Theory]
    [InlineData("Pendiente")]
    [InlineData("Confirmada")]
    public async Task Delete_ProductoConOrdenActivaEnOrders_Devuelve409ConPRD004(string estado)
    {
        var id = await CrearProductoAsync();
        _factory.OrdersApi.AgregarOrden(id, estado);

        var response = await _client.DeleteAsync($"/api/products/{id}");

        var body = await ErrorContractAssert.IsErrorAsync(response, HttpStatusCode.Conflict, ErrorCodes.PRD_004,
            instance: $"/api/products/{id}",
            errorMessage: "El producto tiene órdenes activas y no puede eliminarse.");
        Assert.Equal("No se puede eliminar el recurso.", body.GetProperty("detail").GetString());
    }

    [Fact]
    public async Task Delete_ProductoSoloConOrdenesTerminadas_Devuelve204()
    {
        var id = await CrearProductoAsync();
        _factory.OrdersApi.AgregarOrden(id, "Entregada");
        _factory.OrdersApi.AgregarOrden(id, "Cancelada");

        var response = await _client.DeleteAsync($"/api/products/{id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task Delete_OrdenActivaDeOtroProducto_NoBloqueaElBorrado()
    {
        var id = await CrearProductoAsync();
        _factory.OrdersApi.AgregarOrden(Guid.NewGuid(), "Pendiente");

        var response = await _client.DeleteAsync($"/api/products/{id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task Delete_OrdersCaido_Devuelve500ConPRD005YNoBorraElProducto()
    {
        // D-39: si no se puede saber si hay órdenes activas, no se borra.
        var id = await CrearProductoAsync();
        _factory.OrdersApi.Caido = true;

        var response = await _client.DeleteAsync($"/api/products/{id}");

        await ErrorContractAssert.IsErrorAsync(response, HttpStatusCode.InternalServerError, ErrorCodes.PRD_005,
            instance: $"/api/products/{id}");
        Assert.Equal(HttpStatusCode.OK, (await _client.GetAsync($"/api/products/{id}")).StatusCode);
    }

    [Fact]
    public async Task Delete_PropagaElCorrelationIdDelRequestALaLlamadaAOrders()
    {
        var id = await CrearProductoAsync();
        var request = new HttpRequestMessage(HttpMethod.Delete, $"/api/products/{id}");
        request.Headers.Add(Header, "demo-etapa-9");

        await _client.SendAsync(request);

        Assert.Equal("demo-etapa-9", Assert.Single(_factory.OrdersApi.CorrelationIdsRecibidos));
    }

    [Fact]
    public async Task Delete_SinHeader_PropagaElCorrelationIdGenerado()
    {
        var id = await CrearProductoAsync();

        var response = await _client.DeleteAsync($"/api/products/{id}");

        var generado = response.Headers.GetValues(Header).Single();
        Assert.Equal(generado, Assert.Single(_factory.OrdersApi.CorrelationIdsRecibidos));
    }

    private async Task<Guid> CrearProductoAsync()
    {
        var response = await _client.PostAsJsonAsync("/api/products", new
        {
            nombre = $"Producto de prueba {Guid.NewGuid():N}",
            precio = 10.50m,
            stock = 5,
            categoria = "Otros"
        });
        response.EnsureSuccessStatusCode();

        var creado = await response.Content.ReadFromJsonAsync<CreatedProduct>();
        return creado!.Id;
    }

    private sealed record CreatedProduct(Guid Id);

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
    }
}
