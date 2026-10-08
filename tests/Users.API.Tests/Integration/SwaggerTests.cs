using System.Net;
using System.Text.Json;
using Users.API.Exceptions;

namespace Users.API.Tests.Integration;

/// <summary>
/// Requisitos de la sección 5.1 del enunciado aplicados a la tabla de endpoints de Users (sección 4.2 y D-06).
/// </summary>
public class SwaggerTests(UsersApiFactory factory) : IClassFixture<UsersApiFactory>, IAsyncLifetime
{
    private const string Register = "/api/users/register";
    private const string Login = "/api/users/login";
    private const string ById = "/api/users/{id}";

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

    // Tabla "Endpoints" de la sección 4.2 del enunciado, más GET /api/users/{id} (D-06).
    [Theory]
    [InlineData(Register, "post", new[] { "201", "400", "409", "500" })]
    [InlineData(Login, "post", new[] { "200", "400", "401", "403", "500" })]
    [InlineData(ById, "get", new[] { "200", "404", "500" })]
    public void Endpoint_DocumentaExactamenteLosStatusDelContrato(string path, string method, string[] statusEsperados)
    {
        var documentados = Operation(path, method).GetProperty("responses").EnumerateObject().Select(r => r.Name).Order();

        Assert.Equal(statusEsperados.Order(), documentados);
    }

    [Theory]
    [InlineData(Register, "post")]
    [InlineData(Login, "post")]
    [InlineData(ById, "get")]
    public void Endpoint_TieneResumenDeXmlCommentsYTagUsers(string path, string method)
    {
        var operation = Operation(path, method);

        Assert.False(string.IsNullOrWhiteSpace(operation.GetProperty("summary").GetString()));
        Assert.Equal("Users", operation.GetProperty("tags")[0].GetString());
    }

    // Catálogo de errores de la sección 4.2 (y USR-007, sección 5.1 del plan): cada error con un ejemplo del contrato.
    [Theory]
    [InlineData(Register, "post", "409", ErrorCodes.USR_001)]
    [InlineData(Register, "post", "400", ErrorCodes.USR_002)]
    [InlineData(Login, "post", "401", ErrorCodes.USR_003)]
    [InlineData(Login, "post", "403", ErrorCodes.USR_004)]
    [InlineData(Login, "post", "403", ErrorCodes.USR_005)]
    [InlineData(ById, "get", "500", ErrorCodes.USR_006)]
    [InlineData(ById, "get", "404", ErrorCodes.USR_007)]
    public void RespuestaDeError_TieneEjemploConElErrorCode(string path, string method, string status, string errorCode)
    {
        var ejemplo = Operation(path, method).GetProperty("responses").GetProperty(status).GetProperty("content")
            .GetProperty("application/problem+json").GetProperty("examples").GetProperty(errorCode).GetProperty("value");

        Assert.Equal(errorCode, ejemplo.GetProperty("errorCode").GetString());
        Assert.Equal(int.Parse(status), ejemplo.GetProperty("status").GetInt32());
        Assert.DoesNotContain("{", ejemplo.GetProperty("instance").GetString());
    }

    [Fact]
    public void UserResponse_TieneEjemplosDeXmlCommentsYNoExponeElHash()
    {
        var properties = _document.GetProperty("components").GetProperty("schemas")
            .GetProperty("UserResponse").GetProperty("properties");

        Assert.Equal("a1b2c3d4-0000-0000-0000-111122223333", properties.GetProperty("id").GetProperty("example").GetString());
        Assert.False(properties.TryGetProperty("passwordHash", out _));
    }

    private JsonElement Operation(string path, string method) =>
        _document.GetProperty("paths").GetProperty(path).GetProperty(method);
}
