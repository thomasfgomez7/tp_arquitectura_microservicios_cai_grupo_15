using System.Net;
using System.Text.Json;
using Orders.API.Exceptions;

namespace Orders.API.Tests.Integration;

/// <summary>
/// Requisitos de la sección 5.1 del enunciado aplicados a la tabla de endpoints de Orders (sección 4.3).
/// </summary>
public class SwaggerTests(OrdersApiFactory factory) : IClassFixture<OrdersApiFactory>, IAsyncLifetime
{
    private const string Orders = "/api/orders";
    private const string Order = "/api/orders/{id}";
    private const string Status = "/api/orders/{id}/status";

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

    // Tabla "Endpoints" de la sección 4.3 del enunciado. El POST lista 409, pero el catálogo no tiene
    // ningún error 409 al crear una orden: no se devuelve ni se documenta (D-38).
    [Theory]
    [InlineData(Orders, "get", new[] { "200", "500" })]
    [InlineData(Order, "get", new[] { "200", "404", "500" })]
    [InlineData(Orders, "post", new[] { "201", "400", "404", "422", "500" })]
    [InlineData(Status, "put", new[] { "200", "400", "404", "409", "500" })]
    public void Endpoint_DocumentaExactamenteLosStatusDelContrato(string path, string method, string[] statusEsperados)
    {
        var documentados = Operation(path, method).GetProperty("responses").EnumerateObject().Select(r => r.Name).Order();

        Assert.Equal(statusEsperados.Order(), documentados);
    }

    [Theory]
    [InlineData(Orders, "get")]
    [InlineData(Order, "get")]
    [InlineData(Orders, "post")]
    [InlineData(Status, "put")]
    public void Endpoint_TieneResumenDeXmlCommentsYTagOrders(string path, string method)
    {
        var operation = Operation(path, method);

        Assert.False(string.IsNullOrWhiteSpace(operation.GetProperty("summary").GetString()));
        Assert.Equal("Orders", operation.GetProperty("tags")[0].GetString());
    }

    // Catálogo de errores de la sección 4.3: cada error documentado con un ejemplo del contrato.
    [Theory]
    [InlineData(Order, "get", "404", ErrorCodes.ORD_001)]
    [InlineData(Orders, "post", "400", ErrorCodes.ORD_002)]
    [InlineData(Orders, "post", "404", ErrorCodes.ORD_003)]
    [InlineData(Orders, "post", "404", ErrorCodes.ORD_004)]
    [InlineData(Orders, "post", "422", ErrorCodes.ORD_005)]
    [InlineData(Status, "put", "400", ErrorCodes.ORD_002)]
    [InlineData(Status, "put", "404", ErrorCodes.ORD_001)]
    [InlineData(Status, "put", "409", ErrorCodes.ORD_006)]
    [InlineData(Orders, "get", "500", ErrorCodes.ORD_007)]
    public void RespuestaDeError_TieneEjemploConElErrorCode(string path, string method, string status, string errorCode)
    {
        var ejemplo = Operation(path, method).GetProperty("responses").GetProperty(status).GetProperty("content")
            .GetProperty("application/problem+json").GetProperty("examples").GetProperty(errorCode).GetProperty("value");

        Assert.Equal(errorCode, ejemplo.GetProperty("errorCode").GetString());
        Assert.Equal(int.Parse(status), ejemplo.GetProperty("status").GetInt32());
        Assert.DoesNotContain("{", ejemplo.GetProperty("instance").GetString());
    }

    [Fact]
    public void OrderResponse_TieneEjemplosDeXmlComments()
    {
        var properties = _document.GetProperty("components").GetProperty("schemas")
            .GetProperty("OrderResponse").GetProperty("properties");

        Assert.Equal("f1e2d3c4-0000-0000-0000-aabbccddeeff", properties.GetProperty("id").GetProperty("example").GetString());
        Assert.Equal("Pendiente", properties.GetProperty("estado").GetProperty("example").GetString());
    }

    private JsonElement Operation(string path, string method) =>
        _document.GetProperty("paths").GetProperty(path).GetProperty(method);
}
