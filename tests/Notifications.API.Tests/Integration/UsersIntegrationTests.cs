using System.Net;
using System.Net.Http.Json;
using Notifications.API.Exceptions;

namespace Notifications.API.Tests.Integration;

/// <summary>
/// Notifications.API colaborando con Users.API (Etapa 9): Users caído con el UsersClient real
/// y propagación del Correlation ID. Cada test usa su propio factory porque modifica el Users falso.
/// </summary>
public class UsersIntegrationTests : IDisposable
{
    private const string Header = "X-Correlation-Id";
    private const string SendUrl = "/api/notifications/send";

    private readonly NotificationsApiFactory _factory = new();
    private readonly HttpClient _client;

    public UsersIntegrationTests() => _client = _factory.CreateClient();

    [Fact]
    public async Task Send_UsersCaido_Devuelve500ConNTF004()
    {
        // D-29: una falla de Users.API es un error de infraestructura.
        _factory.UsersApi.Caido = true;

        var response = await _client.PostAsJsonAsync(SendUrl, Notificacion());

        await ErrorContractAssert.IsErrorAsync(response, HttpStatusCode.InternalServerError, ErrorCodes.NTF_004,
            instance: SendUrl, errorMessage: "Error interno al procesar la notificación.");
    }

    [Fact]
    public async Task GetByUser_UsersCaido_SigueFuncionando()
    {
        // D-30: listar no consulta a Users, así que no depende de que esté levantado.
        await _client.PostAsJsonAsync(SendUrl, Notificacion());
        _factory.UsersApi.Caido = true;

        var response = await _client.GetAsync($"/api/notifications/{FakeUsersApi.Maria}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Send_PropagaElCorrelationIdDelRequestALaLlamadaAUsers()
    {
        var request = new HttpRequestMessage(HttpMethod.Post, SendUrl) { Content = JsonContent.Create(Notificacion()) };
        request.Headers.Add(Header, "demo-etapa-9");

        await _client.SendAsync(request);

        Assert.Equal("demo-etapa-9", Assert.Single(_factory.UsersApi.CorrelationIdsRecibidos));
    }

    [Fact]
    public async Task Send_SinHeader_PropagaElCorrelationIdGenerado()
    {
        var response = await _client.PostAsJsonAsync(SendUrl, Notificacion());

        var generado = response.Headers.GetValues(Header).Single();
        Assert.Equal(generado, Assert.Single(_factory.UsersApi.CorrelationIdsRecibidos));
    }

    private static object Notificacion() => new { usuarioId = FakeUsersApi.Maria, mensaje = "Hola", tipo = "Email" };

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
    }
}
