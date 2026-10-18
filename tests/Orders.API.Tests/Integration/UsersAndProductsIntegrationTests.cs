using System.Net;
using System.Net.Http.Json;
using Orders.API.DTOs;
using Orders.API.Exceptions;

namespace Orders.API.Tests.Integration;

/// <summary>
/// Orders.API colaborando con Users.API y Products.API (Etapa 9): servicios caídos con los clientes reales
/// y propagación del Correlation ID. Cada test usa su propio factory porque modifica los servicios falsos.
/// </summary>
public class UsersAndProductsIntegrationTests : IDisposable
{
    private const string Header = "X-Correlation-Id";

    private readonly OrdersApiFactory _factory = new();
    private readonly HttpClient _client;

    public UsersAndProductsIntegrationTests() => _client = _factory.CreateClient();

    [Theory]
    [InlineData("Users.API")]
    [InlineData("Products.API")]
    public async Task Create_ServicioCaido_Devuelve500ConORD007(string servicio)
    {
        // D-36: una falla de otro servicio es un error de infraestructura.
        Caer(servicio);

        var response = await _client.PostAsJsonAsync("/api/orders", Orden());

        await ErrorContractAssert.IsErrorAsync(response, HttpStatusCode.InternalServerError, ErrorCodes.ORD_007,
            instance: "/api/orders", errorMessage: "Error interno al procesar la orden.");
    }

    [Fact]
    public async Task ConsultarYCambiarEstado_ConUsersYProductsCaidos_SigueFuncionando()
    {
        // Solo crear una orden consulta a otros servicios.
        var creada = await _client.PostAsJsonAsync("/api/orders", Orden());
        var orden = await creada.Content.ReadFromJsonAsync<OrderResponse>();
        Caer("Users.API");
        Caer("Products.API");

        var get = await _client.GetAsync($"/api/orders/{orden!.Id}");
        var put = await _client.PutAsJsonAsync($"/api/orders/{orden.Id}/status", new { estado = "Confirmada" });

        Assert.Equal(HttpStatusCode.OK, get.StatusCode);
        Assert.Equal(HttpStatusCode.OK, put.StatusCode);
    }

    [Fact]
    public async Task Create_PropagaElCorrelationIdDelRequestAUsersYAProducts()
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/orders") { Content = JsonContent.Create(Orden()) };
        request.Headers.Add(Header, "demo-etapa-9");

        await _client.SendAsync(request);

        Assert.Equal("demo-etapa-9", Assert.Single(_factory.UsersApi.CorrelationIdsRecibidos));
        Assert.Equal("demo-etapa-9", Assert.Single(_factory.ProductsApi.CorrelationIdsRecibidos));
    }

    [Fact]
    public async Task Create_SinHeader_PropagaElCorrelationIdGenerado()
    {
        var response = await _client.PostAsJsonAsync("/api/orders", Orden());

        var generado = response.Headers.GetValues(Header).Single();
        Assert.Equal(generado, Assert.Single(_factory.UsersApi.CorrelationIdsRecibidos));
        Assert.Equal(generado, Assert.Single(_factory.ProductsApi.CorrelationIdsRecibidos));
    }

    private void Caer(string servicio)
    {
        if (servicio == "Users.API")
        {
            _factory.UsersApi.Caido = true;
        }
        else
        {
            _factory.ProductsApi.Caido = true;
        }
    }

    private static object Orden() => new
    {
        usuarioId = FakeUsersApi.Maria,
        items = new[] { new { productoId = FakeProductsApi.Notebook, cantidad = 1 } }
    };

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
    }
}
