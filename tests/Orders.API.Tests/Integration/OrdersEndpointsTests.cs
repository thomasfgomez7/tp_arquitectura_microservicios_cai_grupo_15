using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Orders.API.Clients;
using Orders.API.DTOs;
using Orders.API.Exceptions;

namespace Orders.API.Tests.Integration;

/// <summary>
/// Contrato de la sección 4.3 del enunciado. Todos los tests comparten la misma API en memoria:
/// cada uno crea sus propias órdenes y no asume que la lista arranca vacía.
/// </summary>
public class OrdersEndpointsTests(OrdersApiFactory factory) : IClassFixture<OrdersApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    // ---------- GET /api/orders ----------

    [Fact]
    public async Task GetAll_SinFiltros_Devuelve200ConLaOrdenCreada()
    {
        var orden = await CrearOrdenAsync(FakeProductsClient.Notebook, 1);

        var response = await _client.GetAsync("/api/orders");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var ordenes = await response.Content.ReadFromJsonAsync<List<OrderResponse>>();
        Assert.Contains(ordenes!, o => o.Id == orden.Id);
    }

    [Fact]
    public async Task GetAll_FiltroPorUsuario_DevuelveSoloSusOrdenes()
    {
        var orden = await CrearOrdenAsync(FakeProductsClient.Notebook, 1);

        var response = await _client.GetAsync($"/api/orders?usuarioId={FakeUsersClient.Maria}");

        var ordenes = await response.Content.ReadFromJsonAsync<List<OrderResponse>>();
        Assert.Contains(ordenes!, o => o.Id == orden.Id);
        Assert.All(ordenes!, o => Assert.Equal(FakeUsersClient.Maria, o.UsuarioId));
    }

    [Fact]
    public async Task GetAll_UsuarioSinOrdenes_Devuelve200ConListaVacia()
    {
        var response = await _client.GetAsync($"/api/orders?usuarioId={Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Empty((await response.Content.ReadFromJsonAsync<List<OrderResponse>>())!);
    }

    [Fact]
    public async Task GetAll_FiltroPorProducto_DevuelveLasOrdenesQueLoIncluyen()
    {
        // D-07: lo usa Products.API para saber si un producto tiene órdenes activas (PRD-004).
        var orden = await CrearOrdenAsync(FakeProductsClient.Auriculares, 1);

        var response = await _client.GetAsync($"/api/orders?productoId={FakeProductsClient.Auriculares}");

        var ordenes = await response.Content.ReadFromJsonAsync<List<OrderResponse>>();
        Assert.Contains(ordenes!, o => o.Id == orden.Id);
        Assert.All(ordenes!, o => Assert.Contains(o.Items, i => i.ProductoId == FakeProductsClient.Auriculares));
    }

    [Theory]
    [InlineData("usuarioId=abc")]
    [InlineData("productoId=abc")]
    public async Task GetAll_FiltroQueNoEsGuid_Devuelve200ConListaVacia(string query)
    {
        // D-37: el endpoint solo admite 200 y 500 (D-12), así que un filtro inválido no coincide con nada.
        await CrearOrdenAsync(FakeProductsClient.Notebook, 1);

        var response = await _client.GetAsync($"/api/orders?{query}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Empty((await response.Content.ReadFromJsonAsync<List<OrderResponse>>())!);
    }

    // ---------- GET /api/orders/{id} ----------

    [Fact]
    public async Task GetById_OrdenExistente_Devuelve200ConElFormatoDelEnunciado()
    {
        var orden = await CrearOrdenAsync(FakeProductsClient.Notebook, 2);

        var response = await _client.GetAsync($"/api/orders/{orden.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var raiz = body.RootElement;
        Assert.Equal(orden.Id.ToString(), raiz.GetProperty("id").GetString());
        Assert.Equal(FakeUsersClient.Maria.ToString(), raiz.GetProperty("usuarioId").GetString());
        Assert.Equal("Pendiente", raiz.GetProperty("estado").GetString());
        Assert.Equal(3000m, raiz.GetProperty("total").GetDecimal());
        Assert.True(raiz.TryGetProperty("fechaCreacion", out _));
        var item = raiz.GetProperty("items")[0];
        Assert.Equal(FakeProductsClient.Notebook.ToString(), item.GetProperty("productoId").GetString());
        Assert.Equal(2, item.GetProperty("cantidad").GetInt32());
        Assert.Equal(1500m, item.GetProperty("precioUnitario").GetDecimal());
    }

    [Fact]
    public async Task GetById_OrdenInexistente_Devuelve404ConORD001()
    {
        var id = Guid.NewGuid();

        var response = await _client.GetAsync($"/api/orders/{id}");

        await ErrorContractAssert.IsErrorAsync(response, HttpStatusCode.NotFound, ErrorCodes.ORD_001,
            instance: $"/api/orders/{id}", errorMessage: "Orden no encontrada.");
    }

    [Fact]
    public async Task GetById_IdQueNoEsGuid_Devuelve404ConORD001()
    {
        // D-17: un id mal formado es "no encontrado", con su errorCode.
        var response = await _client.GetAsync("/api/orders/99");

        await ErrorContractAssert.IsErrorAsync(response, HttpStatusCode.NotFound, ErrorCodes.ORD_001, instance: "/api/orders/99");
    }

    // ---------- POST /api/orders ----------

    [Fact]
    public async Task Create_DatosValidos_Devuelve201ConLocationYElTotalCalculado()
    {
        var response = await _client.PostAsJsonAsync("/api/orders",
            Crear(FakeUsersClient.Maria, (FakeProductsClient.Notebook, 2), (FakeProductsClient.Auriculares, 1)));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var orden = await response.Content.ReadFromJsonAsync<OrderResponse>();
        Assert.Equal("Pendiente", orden!.Estado);
        Assert.Equal(3350m, orden.Total);
        Assert.EndsWith($"/api/orders/{orden.Id}", response.Headers.Location!.ToString());
    }

    [Fact]
    public async Task Create_SinItems_Devuelve400ConORD002()
    {
        var response = await _client.PostAsJsonAsync("/api/orders", Crear(FakeUsersClient.Maria));

        await ErrorContractAssert.IsErrorAsync(response, HttpStatusCode.BadRequest, ErrorCodes.ORD_002,
            instance: "/api/orders", errorMessage: "La orden debe tener al menos un item.");
    }

    [Fact]
    public async Task Create_BodyVacio_Devuelve400ConORD002YTodosLosProblemas()
    {
        var response = await _client.PostAsync("/api/orders", Json("{}"));

        var body = await ErrorContractAssert.IsErrorAsync(response, HttpStatusCode.BadRequest, ErrorCodes.ORD_002, instance: "/api/orders");
        var mensaje = body.GetProperty("errorMessage").GetString();
        Assert.Contains("El usuario es obligatorio", mensaje);
        Assert.Contains("La orden debe tener al menos un item", mensaje);
    }

    [Fact]
    public async Task Create_ItemConCantidadCero_Devuelve400ConORD002()
    {
        var response = await _client.PostAsJsonAsync("/api/orders", Crear(FakeUsersClient.Maria, (FakeProductsClient.Notebook, 0)));

        await ErrorContractAssert.IsErrorAsync(response, HttpStatusCode.BadRequest, ErrorCodes.ORD_002,
            instance: "/api/orders", errorMessage: "La cantidad debe ser mayor a cero.");
    }

    [Fact]
    public async Task Create_JsonInvalido_Devuelve400ConORD002()
    {
        var response = await _client.PostAsync("/api/orders", Json("{ esto no es json"));

        await ErrorContractAssert.IsErrorAsync(response, HttpStatusCode.BadRequest, ErrorCodes.ORD_002,
            instance: "/api/orders", errorMessage: "El cuerpo de la solicitud no es un JSON válido.");
    }

    [Fact]
    public async Task Create_UsuarioInexistente_Devuelve404ConORD003()
    {
        var response = await _client.PostAsJsonAsync("/api/orders", Crear(Guid.NewGuid(), (FakeProductsClient.Notebook, 1)));

        await ErrorContractAssert.IsErrorAsync(response, HttpStatusCode.NotFound, ErrorCodes.ORD_003,
            instance: "/api/orders", errorMessage: "Usuario no encontrado al crear la orden.");
    }

    [Fact]
    public async Task Create_ProductoInexistente_Devuelve404ConORD004()
    {
        var response = await _client.PostAsJsonAsync("/api/orders", Crear(FakeUsersClient.Maria, (Guid.NewGuid(), 1)));

        await ErrorContractAssert.IsErrorAsync(response, HttpStatusCode.NotFound, ErrorCodes.ORD_004,
            instance: "/api/orders", errorMessage: "Producto no encontrado al crear la orden.");
    }

    [Fact]
    public async Task Create_StockInsuficiente_Devuelve422ConORD005()
    {
        var response = await _client.PostAsJsonAsync("/api/orders", Crear(FakeUsersClient.Maria, (FakeProductsClient.Taladro, 5)));

        await ErrorContractAssert.IsErrorAsync(response, HttpStatusCode.UnprocessableEntity, ErrorCodes.ORD_005,
            instance: "/api/orders", errorMessage: "Stock insuficiente para 'Taladro percutor 750W'. Disponible: 2, solicitado: 5.");
    }

    [Fact]
    public async Task Create_UsersApiNoResponde_Devuelve500ConORD007()
    {
        // D-36: una falla de otro servicio no es un error del negocio.
        var usersClient = Substitute.For<IUsersClient>();
        usersClient.GetUserAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Connection refused"));
        var client = factory.WithWebHostBuilder(b => b.ConfigureTestServices(s => s.AddSingleton(usersClient))).CreateClient();

        var response = await client.PostAsJsonAsync("/api/orders", Crear(FakeUsersClient.Maria, (FakeProductsClient.Notebook, 1)));

        await ErrorContractAssert.IsErrorAsync(response, HttpStatusCode.InternalServerError, ErrorCodes.ORD_007,
            instance: "/api/orders", errorMessage: "Error interno al procesar la orden.");
    }

    // ---------- PUT /api/orders/{id}/status ----------

    [Fact]
    public async Task UpdateStatus_TransicionValida_Devuelve200YLaOrdenQuedaConfirmada()
    {
        var orden = await CrearOrdenAsync(FakeProductsClient.Notebook, 1);

        var response = await _client.PutAsJsonAsync($"/api/orders/{orden.Id}/status", Estado("Confirmada"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var estado = await response.Content.ReadFromJsonAsync<OrderStatusResponse>();
        Assert.Equal(orden.Id, estado!.Id);
        Assert.Equal("Confirmada", estado.Estado);
        var get = await _client.GetFromJsonAsync<OrderResponse>($"/api/orders/{orden.Id}");
        Assert.Equal("Confirmada", get!.Estado);
    }

    [Fact]
    public async Task UpdateStatus_CicloCompleto_LlegaAEntregada()
    {
        var orden = await CrearOrdenAsync(FakeProductsClient.Notebook, 1);

        foreach (var estado in new[] { "Confirmada", "Enviada", "Entregada" })
        {
            var response = await _client.PutAsJsonAsync($"/api/orders/{orden.Id}/status", Estado(estado));
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        var get = await _client.GetFromJsonAsync<OrderResponse>($"/api/orders/{orden.Id}");
        Assert.Equal("Entregada", get!.Estado);
    }

    [Fact]
    public async Task UpdateStatus_TransicionInvalida_Devuelve409ConORD006()
    {
        var orden = await CrearOrdenAsync(FakeProductsClient.Notebook, 1);

        var response = await _client.PutAsJsonAsync($"/api/orders/{orden.Id}/status", Estado("Entregada"));

        await ErrorContractAssert.IsErrorAsync(response, HttpStatusCode.Conflict, ErrorCodes.ORD_006,
            instance: $"/api/orders/{orden.Id}/status",
            errorMessage: "Una orden en estado 'Pendiente' no puede pasar a 'Entregada'.");
    }

    [Fact]
    public async Task UpdateStatus_EstadoDesconocido_Devuelve400ConORD002()
    {
        var orden = await CrearOrdenAsync(FakeProductsClient.Notebook, 1);

        var response = await _client.PutAsJsonAsync($"/api/orders/{orden.Id}/status", Estado("Pagada"));

        await ErrorContractAssert.IsErrorAsync(response, HttpStatusCode.BadRequest, ErrorCodes.ORD_002,
            instance: $"/api/orders/{orden.Id}/status",
            errorMessage: "El estado debe ser Pendiente, Confirmada, Enviada, Entregada o Cancelada.");
    }

    [Fact]
    public async Task UpdateStatus_SinEstado_Devuelve400ConORD002()
    {
        var orden = await CrearOrdenAsync(FakeProductsClient.Notebook, 1);

        var response = await _client.PutAsync($"/api/orders/{orden.Id}/status", Json("{}"));

        await ErrorContractAssert.IsErrorAsync(response, HttpStatusCode.BadRequest, ErrorCodes.ORD_002,
            instance: $"/api/orders/{orden.Id}/status", errorMessage: "El estado es obligatorio.");
    }

    [Fact]
    public async Task UpdateStatus_OrdenInexistente_Devuelve404ConORD001()
    {
        var id = Guid.NewGuid();

        var response = await _client.PutAsJsonAsync($"/api/orders/{id}/status", Estado("Confirmada"));

        await ErrorContractAssert.IsErrorAsync(response, HttpStatusCode.NotFound, ErrorCodes.ORD_001,
            instance: $"/api/orders/{id}/status", errorMessage: "Orden no encontrada.");
    }

    [Fact]
    public async Task UpdateStatus_IdQueNoEsGuid_Devuelve404ConORD001()
    {
        var response = await _client.PutAsJsonAsync("/api/orders/abc/status", Estado("Confirmada"));

        await ErrorContractAssert.IsErrorAsync(response, HttpStatusCode.NotFound, ErrorCodes.ORD_001, instance: "/api/orders/abc/status");
    }

    // ---------- Helpers ----------

    private static CreateOrderRequest Crear(Guid usuarioId, params (Guid ProductoId, int Cantidad)[] items) => new()
    {
        UsuarioId = usuarioId,
        Items = items.Select(item => new CreateOrderItemRequest { ProductoId = item.ProductoId, Cantidad = item.Cantidad }).ToList()
    };

    private static UpdateOrderStatusRequest Estado(string estado) => new() { Estado = estado };

    private static StringContent Json(string body) => new(body, Encoding.UTF8, "application/json");

    private async Task<OrderResponse> CrearOrdenAsync(Guid productoId, int cantidad)
    {
        var response = await _client.PostAsJsonAsync("/api/orders", Crear(FakeUsersClient.Maria, (productoId, cantidad)));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<OrderResponse>())!;
    }
}
