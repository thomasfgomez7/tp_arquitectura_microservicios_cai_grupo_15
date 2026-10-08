using System.Net;
using System.Net.Http.Json;
using System.Text;
using Cart.API.Clients;
using Cart.API.DTOs;
using Cart.API.Exceptions;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace Cart.API.Tests.Integration;

/// <summary>
/// Contrato de la sección 4.4 del enunciado. Cada test usa un usuario nuevo (salvo el de los datos
/// semilla, que nadie modifica) porque todos comparten la misma API en memoria.
/// </summary>
public class CartEndpointsTests(CartApiFactory factory) : IClassFixture<CartApiFactory>
{
    // Carrito de los datos semilla (CartSeedData), igual al ejemplo del enunciado.
    private const string UsuarioSemilla = "a1b2c3d4-0000-0000-0000-111122223333";

    private readonly HttpClient _client = factory.CreateClient();

    // ---------- GET /api/cart/{userId} ----------

    [Fact]
    public async Task Get_CarritoDeLosDatosSemilla_Devuelve200ConSusItems()
    {
        var response = await _client.GetAsync($"/api/cart/{UsuarioSemilla}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var carrito = await response.Content.ReadFromJsonAsync<CartResponse>();
        Assert.Equal(Guid.Parse(UsuarioSemilla), carrito!.UsuarioId);
        Assert.Contains(carrito.Items, i => i.ProductoId == FakeProductsClient.Notebook && i.Cantidad == 1);
        Assert.Contains(carrito.Items, i => i.ProductoId == FakeProductsClient.Auriculares && i.Cantidad == 3);
    }

    [Fact]
    public async Task Get_UsuarioSinCarrito_Devuelve404ConCRT001()
    {
        var usuario = Guid.NewGuid();

        var response = await _client.GetAsync($"/api/cart/{usuario}");

        await ErrorContractAssert.IsErrorAsync(response, HttpStatusCode.NotFound, ErrorCodes.CRT_001,
            instance: $"/api/cart/{usuario}", errorMessage: "Carrito no encontrado.");
    }

    [Fact]
    public async Task Get_UserIdQueNoEsGuid_Devuelve404ConCRT001()
    {
        // D-17: un id mal formado es "no encontrado", con su errorCode.
        var response = await _client.GetAsync("/api/cart/abc");

        await ErrorContractAssert.IsErrorAsync(response, HttpStatusCode.NotFound, ErrorCodes.CRT_001, instance: "/api/cart/abc");
    }

    // ---------- POST /api/cart/{userId}/items ----------

    [Fact]
    public async Task AddItem_UsuarioSinCarrito_Devuelve200ConElCarritoCreado()
    {
        var usuario = Guid.NewGuid();

        var response = await _client.PostAsJsonAsync($"/api/cart/{usuario}/items", Agregar(FakeProductsClient.Notebook, 2));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var carrito = await response.Content.ReadFromJsonAsync<CartResponse>();
        Assert.Equal(usuario, carrito!.UsuarioId);
        var item = Assert.Single(carrito.Items);
        Assert.Equal(2, item.Cantidad);
    }

    [Fact]
    public async Task AddItem_ProductoInexistente_Devuelve404ConCRT002()
    {
        var usuario = Guid.NewGuid();

        var response = await _client.PostAsJsonAsync($"/api/cart/{usuario}/items", Agregar(Guid.NewGuid(), 1));

        await ErrorContractAssert.IsErrorAsync(response, HttpStatusCode.NotFound, ErrorCodes.CRT_002,
            instance: $"/api/cart/{usuario}/items", errorMessage: "Producto no encontrado.");
    }

    [Fact]
    public async Task AddItem_StockInsuficiente_Devuelve422ConCRT003()
    {
        var usuario = Guid.NewGuid();

        var response = await _client.PostAsJsonAsync($"/api/cart/{usuario}/items", Agregar(FakeProductsClient.Taladro, 5));

        var body = await ErrorContractAssert.IsErrorAsync(response, HttpStatusCode.UnprocessableEntity, ErrorCodes.CRT_003,
            instance: $"/api/cart/{usuario}/items", errorMessage: "Stock insuficiente. Disponible: 2, solicitado: 5.");
        Assert.Equal("Unprocessable Entity", body.GetProperty("title").GetString());
        Assert.Equal("No se puede procesar la solicitud.", body.GetProperty("detail").GetString());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-3)]
    public async Task AddItem_CantidadMenorOIgualACero_Devuelve400ConCRT004(int cantidad)
    {
        var usuario = Guid.NewGuid();

        var response = await _client.PostAsJsonAsync($"/api/cart/{usuario}/items", Agregar(FakeProductsClient.Notebook, cantidad));

        await ErrorContractAssert.IsErrorAsync(response, HttpStatusCode.BadRequest, ErrorCodes.CRT_004,
            instance: $"/api/cart/{usuario}/items", errorMessage: "La cantidad debe ser mayor a cero.");
    }

    [Fact]
    public async Task AddItem_SinProductoNiCantidad_Devuelve400ConCRT004YAmbosProblemas()
    {
        // D-25: CRT-004 es el único 400 del catálogo; el mensaje lista todos los problemas.
        var usuario = Guid.NewGuid();

        var response = await _client.PostAsJsonAsync($"/api/cart/{usuario}/items", new { });

        var body = await ErrorContractAssert.IsErrorAsync(response, HttpStatusCode.BadRequest, ErrorCodes.CRT_004,
            instance: $"/api/cart/{usuario}/items");
        var mensaje = body.GetProperty("errorMessage").GetString()!;
        Assert.Contains("El producto es obligatorio", mensaje);
        Assert.Contains("La cantidad es obligatoria", mensaje);
    }

    [Fact]
    public async Task AddItem_JsonMalFormado_Devuelve400ConCRT004()
    {
        var usuario = Guid.NewGuid();
        var contenido = new StringContent("{ \"cantidad\": ", Encoding.UTF8, "application/json");

        var response = await _client.PostAsync($"/api/cart/{usuario}/items", contenido);

        await ErrorContractAssert.IsErrorAsync(response, HttpStatusCode.BadRequest, ErrorCodes.CRT_004,
            instance: $"/api/cart/{usuario}/items", errorMessage: "El cuerpo de la solicitud no es un JSON válido.");
    }

    [Fact]
    public async Task AddItem_ProductsNoResponde_Devuelve500ConCRT005()
    {
        // D-28: una falla de Products.API es un error de infraestructura.
        var productsCaido = Substitute.For<IProductsClient>();
        productsCaido.GetProductAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Connection refused"));
        var client = factory.WithWebHostBuilder(b => b.ConfigureTestServices(s => s.AddSingleton(productsCaido))).CreateClient();
        var usuario = Guid.NewGuid();

        var response = await client.PostAsJsonAsync($"/api/cart/{usuario}/items", Agregar(FakeProductsClient.Notebook, 1));

        await ErrorContractAssert.IsErrorAsync(response, HttpStatusCode.InternalServerError, ErrorCodes.CRT_005,
            instance: $"/api/cart/{usuario}/items", errorMessage: "Error interno al procesar el carrito.");
    }

    // ---------- PUT /api/cart/{userId}/items/{productId} ----------

    [Fact]
    public async Task UpdateItem_ProductoEnElCarrito_Devuelve200ConLaCantidadNueva()
    {
        var usuario = await CrearCarritoAsync(FakeProductsClient.Notebook, 1);

        var response = await _client.PutAsJsonAsync($"/api/cart/{usuario}/items/{FakeProductsClient.Notebook}", Cantidad(4));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var carrito = await response.Content.ReadFromJsonAsync<CartResponse>();
        Assert.Equal(4, Assert.Single(carrito!.Items).Cantidad);
    }

    [Fact]
    public async Task UpdateItem_UsuarioSinCarrito_Devuelve404ConCRT001()
    {
        var usuario = Guid.NewGuid();

        var response = await _client.PutAsJsonAsync($"/api/cart/{usuario}/items/{FakeProductsClient.Notebook}", Cantidad(1));

        await ErrorContractAssert.IsErrorAsync(response, HttpStatusCode.NotFound, ErrorCodes.CRT_001,
            instance: $"/api/cart/{usuario}/items/{FakeProductsClient.Notebook}");
    }

    [Fact]
    public async Task UpdateItem_ProductoQueNoEstaEnElCarrito_Devuelve404ConCRT002()
    {
        var usuario = await CrearCarritoAsync(FakeProductsClient.Notebook, 1);

        var response = await _client.PutAsJsonAsync($"/api/cart/{usuario}/items/{FakeProductsClient.Auriculares}", Cantidad(1));

        await ErrorContractAssert.IsErrorAsync(response, HttpStatusCode.NotFound, ErrorCodes.CRT_002,
            instance: $"/api/cart/{usuario}/items/{FakeProductsClient.Auriculares}",
            errorMessage: "El producto no se encuentra en el carrito.");
    }

    [Fact]
    public async Task UpdateItem_StockInsuficiente_Devuelve422ConCRT003()
    {
        var usuario = await CrearCarritoAsync(FakeProductsClient.Taladro, 1);

        var response = await _client.PutAsJsonAsync($"/api/cart/{usuario}/items/{FakeProductsClient.Taladro}", Cantidad(3));

        await ErrorContractAssert.IsErrorAsync(response, HttpStatusCode.UnprocessableEntity, ErrorCodes.CRT_003,
            instance: $"/api/cart/{usuario}/items/{FakeProductsClient.Taladro}",
            errorMessage: "Stock insuficiente. Disponible: 2, solicitado: 3.");
    }

    [Fact]
    public async Task UpdateItem_CantidadCero_Devuelve400ConCRT004()
    {
        var usuario = await CrearCarritoAsync(FakeProductsClient.Notebook, 1);

        var response = await _client.PutAsJsonAsync($"/api/cart/{usuario}/items/{FakeProductsClient.Notebook}", Cantidad(0));

        await ErrorContractAssert.IsErrorAsync(response, HttpStatusCode.BadRequest, ErrorCodes.CRT_004,
            instance: $"/api/cart/{usuario}/items/{FakeProductsClient.Notebook}");
    }

    [Fact]
    public async Task UpdateItem_ProductIdQueNoEsGuid_Devuelve404ConCRT002()
    {
        var usuario = await CrearCarritoAsync(FakeProductsClient.Notebook, 1);

        var response = await _client.PutAsJsonAsync($"/api/cart/{usuario}/items/xyz", Cantidad(1));

        await ErrorContractAssert.IsErrorAsync(response, HttpStatusCode.NotFound, ErrorCodes.CRT_002,
            instance: $"/api/cart/{usuario}/items/xyz");
    }

    // ---------- DELETE /api/cart/{userId}/items/{productId} ----------

    [Fact]
    public async Task RemoveItem_ProductoEnElCarrito_Devuelve204YLoQuita()
    {
        var usuario = await CrearCarritoAsync(FakeProductsClient.Notebook, 1);

        var response = await _client.DeleteAsync($"/api/cart/{usuario}/items/{FakeProductsClient.Notebook}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var carrito = await _client.GetFromJsonAsync<CartResponse>($"/api/cart/{usuario}");
        Assert.Empty(carrito!.Items);
    }

    [Fact]
    public async Task RemoveItem_UsuarioSinCarrito_Devuelve404ConCRT001()
    {
        var usuario = Guid.NewGuid();

        var response = await _client.DeleteAsync($"/api/cart/{usuario}/items/{FakeProductsClient.Notebook}");

        await ErrorContractAssert.IsErrorAsync(response, HttpStatusCode.NotFound, ErrorCodes.CRT_001,
            instance: $"/api/cart/{usuario}/items/{FakeProductsClient.Notebook}");
    }

    [Fact]
    public async Task RemoveItem_ProductoQueNoEstaEnElCarrito_Devuelve404ConCRT002()
    {
        var usuario = await CrearCarritoAsync(FakeProductsClient.Notebook, 1);

        var response = await _client.DeleteAsync($"/api/cart/{usuario}/items/{FakeProductsClient.Auriculares}");

        await ErrorContractAssert.IsErrorAsync(response, HttpStatusCode.NotFound, ErrorCodes.CRT_002,
            instance: $"/api/cart/{usuario}/items/{FakeProductsClient.Auriculares}");
    }

    // ---------- DELETE /api/cart/{userId} ----------

    [Fact]
    public async Task Clear_CarritoExistente_Devuelve204YLuegoGetDevuelveCRT001()
    {
        var usuario = await CrearCarritoAsync(FakeProductsClient.Notebook, 1);

        var response = await _client.DeleteAsync($"/api/cart/{usuario}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var get = await _client.GetAsync($"/api/cart/{usuario}");
        await ErrorContractAssert.IsErrorAsync(get, HttpStatusCode.NotFound, ErrorCodes.CRT_001, instance: $"/api/cart/{usuario}");
    }

    [Fact]
    public async Task Clear_UsuarioSinCarrito_Devuelve404ConCRT001()
    {
        var usuario = Guid.NewGuid();

        var response = await _client.DeleteAsync($"/api/cart/{usuario}");

        await ErrorContractAssert.IsErrorAsync(response, HttpStatusCode.NotFound, ErrorCodes.CRT_001, instance: $"/api/cart/{usuario}");
    }

    // ---------- Helpers ----------

    private static AddCartItemRequest Agregar(Guid productoId, int cantidad) => new() { ProductoId = productoId, Cantidad = cantidad };

    private static UpdateCartItemRequest Cantidad(int cantidad) => new() { Cantidad = cantidad };

    private async Task<Guid> CrearCarritoAsync(Guid productoId, int cantidad)
    {
        var usuario = Guid.NewGuid();
        var response = await _client.PostAsJsonAsync($"/api/cart/{usuario}/items", Agregar(productoId, cantidad));
        response.EnsureSuccessStatusCode();
        return usuario;
    }
}
