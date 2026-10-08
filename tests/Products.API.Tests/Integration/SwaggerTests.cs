using System.Net;
using System.Text.Json;
using Products.API.Exceptions;

namespace Products.API.Tests.Integration;

/// <summary>
/// Requisitos de la sección 5.1 del enunciado: Swagger UI en /swagger, cada endpoint con todos sus
/// códigos de respuesta, ejemplos de éxito y de error con errorCode, XML comments y tags.
/// </summary>
public class SwaggerTests(ProductsApiFactory factory) : IClassFixture<ProductsApiFactory>, IAsyncLifetime
{
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

    // Tabla "Endpoints" de la sección 4.1 del enunciado.
    [Theory]
    [InlineData("/api/products", "get", new[] { "200", "500" })]
    [InlineData("/api/products/{id}", "get", new[] { "200", "404", "500" })]
    [InlineData("/api/products", "post", new[] { "201", "400", "409", "500" })]
    [InlineData("/api/products/{id}", "put", new[] { "200", "400", "404", "500" })]
    [InlineData("/api/products/{id}", "delete", new[] { "204", "404", "409", "500" })]
    public void Endpoint_DocumentaExactamenteLosStatusDelContrato(string path, string method, string[] statusEsperados)
    {
        var responses = Operation(path, method).GetProperty("responses");

        var documentados = responses.EnumerateObject().Select(r => r.Name).Order().ToArray();
        Assert.Equal(statusEsperados.Order().ToArray(), documentados);
    }

    [Theory]
    [InlineData("/api/products", "get")]
    [InlineData("/api/products/{id}", "get")]
    [InlineData("/api/products", "post")]
    [InlineData("/api/products/{id}", "put")]
    [InlineData("/api/products/{id}", "delete")]
    public void Endpoint_TieneResumenDeXmlCommentsYTagProducts(string path, string method)
    {
        var operation = Operation(path, method);

        Assert.False(string.IsNullOrWhiteSpace(operation.GetProperty("summary").GetString()));
        Assert.Equal("Products", operation.GetProperty("tags")[0].GetString());
    }

    // Catálogo de errores de la sección 4.1: cada error documentado con un ejemplo del contrato.
    [Theory]
    [InlineData("/api/products/{id}", "get", "404", ErrorCodes.PRD_001)]
    [InlineData("/api/products", "post", "400", ErrorCodes.PRD_002)]
    [InlineData("/api/products", "post", "409", ErrorCodes.PRD_003)]
    [InlineData("/api/products/{id}", "delete", "409", ErrorCodes.PRD_004)]
    [InlineData("/api/products", "get", "500", ErrorCodes.PRD_005)]
    public void RespuestaDeError_TieneEjemploConElErrorCode(string path, string method, string status, string errorCode)
    {
        var content = Operation(path, method).GetProperty("responses").GetProperty(status).GetProperty("content");
        var ejemplo = content.GetProperty("application/problem+json").GetProperty("examples").GetProperty(errorCode).GetProperty("value");

        Assert.Equal(errorCode, ejemplo.GetProperty("errorCode").GetString());
        Assert.Equal(int.Parse(status), ejemplo.GetProperty("status").GetInt32());
        Assert.False(string.IsNullOrWhiteSpace(ejemplo.GetProperty("errorMessage").GetString()));
        Assert.False(string.IsNullOrWhiteSpace(ejemplo.GetProperty("correlationId").GetString()));
    }

    [Fact]
    public void ProductResponse_TieneEjemplosDeXmlComments()
    {
        var properties = _document.GetProperty("components").GetProperty("schemas")
            .GetProperty("ProductResponse").GetProperty("properties");

        Assert.Equal("Notebook Dell XPS 15", properties.GetProperty("nombre").GetProperty("example").GetString());
        Assert.Equal("Electrónica", properties.GetProperty("categoria").GetProperty("example").GetString());
    }

    [Fact]
    public void CreateProductRequest_TieneEjemplosYDescripciones()
    {
        var properties = _document.GetProperty("components").GetProperty("schemas")
            .GetProperty("CreateProductRequest").GetProperty("properties");

        Assert.Equal("Notebook Dell XPS 15", properties.GetProperty("nombre").GetProperty("example").GetString());
        Assert.False(string.IsNullOrWhiteSpace(properties.GetProperty("precio").GetProperty("description").GetString()));
    }

    private JsonElement Operation(string path, string method) =>
        _document.GetProperty("paths").GetProperty(path).GetProperty(method);
}
