using System.Net;
using System.Text.Json;
using Notifications.API.Exceptions;

namespace Notifications.API.Tests.Integration;

/// <summary>
/// Requisitos de la sección 5.1 del enunciado aplicados a la tabla de endpoints de Notifications (sección 4.5).
/// </summary>
public class SwaggerTests(NotificationsApiFactory factory) : IClassFixture<NotificationsApiFactory>, IAsyncLifetime
{
    private const string Send = "/api/notifications/send";
    private const string ByUser = "/api/notifications/{userId}";

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

    // Tabla "Endpoints" de la sección 4.5 del enunciado.
    [Theory]
    [InlineData(Send, "post", new[] { "201", "400", "404", "500" })]
    [InlineData(ByUser, "get", new[] { "200", "404", "500" })]
    public void Endpoint_DocumentaExactamenteLosStatusDelContrato(string path, string method, string[] statusEsperados)
    {
        var documentados = Operation(path, method).GetProperty("responses").EnumerateObject().Select(r => r.Name).Order();

        Assert.Equal(statusEsperados.Order(), documentados);
    }

    [Theory]
    [InlineData(Send, "post")]
    [InlineData(ByUser, "get")]
    public void Endpoint_TieneResumenDeXmlCommentsYTagNotifications(string path, string method)
    {
        var operation = Operation(path, method);

        Assert.False(string.IsNullOrWhiteSpace(operation.GetProperty("summary").GetString()));
        Assert.Equal("Notifications", operation.GetProperty("tags")[0].GetString());
    }

    // Catálogo de errores de la sección 4.5: cada error documentado con un ejemplo del contrato.
    [Theory]
    [InlineData(Send, "post", "404", ErrorCodes.NTF_001)]
    [InlineData(Send, "post", "400", ErrorCodes.NTF_002)]
    [InlineData(ByUser, "get", "404", ErrorCodes.NTF_003)]
    [InlineData(ByUser, "get", "500", ErrorCodes.NTF_004)]
    public void RespuestaDeError_TieneEjemploConElErrorCode(string path, string method, string status, string errorCode)
    {
        var ejemplo = Operation(path, method).GetProperty("responses").GetProperty(status).GetProperty("content")
            .GetProperty("application/problem+json").GetProperty("examples").GetProperty(errorCode).GetProperty("value");

        Assert.Equal(errorCode, ejemplo.GetProperty("errorCode").GetString());
        Assert.Equal(int.Parse(status), ejemplo.GetProperty("status").GetInt32());
        Assert.DoesNotContain("{", ejemplo.GetProperty("instance").GetString());
    }

    [Fact]
    public void NotificationResponse_TieneEjemplosDeXmlComments()
    {
        var properties = _document.GetProperty("components").GetProperty("schemas")
            .GetProperty("NotificationResponse").GetProperty("properties");

        Assert.Equal("a1b2c3d4-0000-0000-0000-111122223333", properties.GetProperty("usuarioId").GetProperty("example").GetString());
    }

    private JsonElement Operation(string path, string method) =>
        _document.GetProperty("paths").GetProperty(path).GetProperty(method);
}
