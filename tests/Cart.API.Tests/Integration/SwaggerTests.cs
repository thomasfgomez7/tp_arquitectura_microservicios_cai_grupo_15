using System.Net;
using System.Text.Json;
using Cart.API.Exceptions;

namespace Cart.API.Tests.Integration;

/// <summary>
/// Requisitos de la sección 5.1 del enunciado aplicados a la tabla de endpoints de Cart (sección 4.4).
/// </summary>
public class SwaggerTests(CartApiFactory factory) : IClassFixture<CartApiFactory>, IAsyncLifetime
{
    private const string Cart = "/api/cart/{userId}";
    private const string Items = "/api/cart/{userId}/items";
    private const string Item = "/api/cart/{userId}/items/{productId}";

    private readonly HttpClient _client = factory.CreateClient();
    private JsonElement _document;

    public async Task InitializeAsync()
    {
        var response = await _client.GetAsync("/swagger/v1/swagger.json");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        _document = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement.Clone();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task SwaggerUI_EstaDisponibleEnSwagger()
    {
        var response = await _client.GetAsync("/swagger/index.html");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // Tabla "Endpoints" de la sección 4.4 del enunciado.
    [Theory]
    [InlineData(Cart, "get", new[] { "200", "404", "500" })]
    [InlineData(Items, "post", new[] { "200", "400", "404", "422", "500" })]
    [InlineData(Item, "put", new[] { "200", "400", "404", "422", "500" })]
    [InlineData(Item, "delete", new[] { "204", "404", "500" })]
    [InlineData(Cart, "delete", new[] { "204", "404", "500" })]
    public void Endpoint_DocumentaExactamenteLosStatusDelContrato(string path, string method, string[] statusEsperados)
    {
        var documentados = Operation(path, method).GetProperty("responses").EnumerateObject().Select(r => r.Name).Order();

        Assert.Equal(statusEsperados.Order(), documentados);
    }

    [Theory]
    [InlineData(Cart, "get")]
    [InlineData(Items, "post")]
    [InlineData(Item, "put")]
    [InlineData(Item, "delete")]
    [InlineData(Cart, "delete")]
    public void Endpoint_TieneResumenDeXmlCommentsYTagCart(string path, string method)
    {
        var operation = Operation(path, method);

        Assert.False(string.IsNullOrWhiteSpace(operation.GetProperty("summary").GetString()));
        Assert.Equal("Cart", operation.GetProperty("tags")[0].GetString());
    }

    // Catálogo de errores de la sección 4.4: cada error documentado con un ejemplo del contrato.
    [Theory]
    [InlineData(Cart, "get", "404", ErrorCodes.CRT_001)]
    [InlineData(Items, "post", "404", ErrorCodes.CRT_002)]
    [InlineData(Items, "post", "422", ErrorCodes.CRT_003)]
    [InlineData(Items, "post", "400", ErrorCodes.CRT_004)]
    [InlineData(Item, "put", "404", ErrorCodes.CRT_001)]
    [InlineData(Item, "put", "404", ErrorCodes.CRT_002)]
    [InlineData(Cart, "delete", "500", ErrorCodes.CRT_005)]
    public void RespuestaDeError_TieneEjemploConElErrorCode(string path, string method, string status, string errorCode)
    {
        var ejemplo = Operation(path, method).GetProperty("responses").GetProperty(status).GetProperty("content")
            .GetProperty("application/problem+json").GetProperty("examples").GetProperty(errorCode).GetProperty("value");

        Assert.Equal(errorCode, ejemplo.GetProperty("errorCode").GetString());
        Assert.Equal(int.Parse(status), ejemplo.GetProperty("status").GetInt32());
        Assert.DoesNotContain("{", ejemplo.GetProperty("instance").GetString());
    }

    [Fact]
    public void CartResponse_TieneEjemplosDeXmlComments()
    {
        var properties = _document.GetProperty("components").GetProperty("schemas")
            .GetProperty("CartResponse").GetProperty("properties");

        Assert.Equal("a1b2c3d4-0000-0000-0000-111122223333", properties.GetProperty("usuarioId").GetProperty("example").GetString());
    }

    private JsonElement Operation(string path, string method) =>
        _document.GetProperty("paths").GetProperty(path).GetProperty(method);
}
