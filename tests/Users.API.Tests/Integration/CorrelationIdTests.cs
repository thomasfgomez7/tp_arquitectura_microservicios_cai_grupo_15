using System.Net;
using System.Text.Json;

namespace Users.API.Tests.Integration;

public class CorrelationIdTests(UsersApiFactory factory) : IClassFixture<UsersApiFactory>
{
    private const string Header = "X-Correlation-Id";
    private const string MariaUrl = "/api/users/a1b2c3d4-0000-0000-0000-111122223333";

    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Request_SinHeader_GeneraUnCorrelationIdEnLaRespuesta()
    {
        var response = await _client.GetAsync(MariaUrl);

        var correlationId = response.Headers.GetValues(Header).Single();
        Assert.True(Guid.TryParse(correlationId, out _));
    }

    [Fact]
    public async Task Request_SinHeader_GeneraUnIdDistintoPorRequest()
    {
        var primero = await _client.GetAsync(MariaUrl);
        var segundo = await _client.GetAsync(MariaUrl);

        Assert.NotEqual(primero.Headers.GetValues(Header).Single(), segundo.Headers.GetValues(Header).Single());
    }

    [Fact]
    public async Task Request_ConHeader_DevuelveElMismoCorrelationId()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, MariaUrl);
        request.Headers.Add(Header, "demo-123");

        var response = await _client.SendAsync(request);

        Assert.Equal("demo-123", response.Headers.GetValues(Header).Single());
    }

    [Fact]
    public async Task Request_ConHeaderInvalido_LoReemplazaPorUnoNuevo()
    {
        // Un valor demasiado largo o con caracteres raros podría ensuciar los logs: se descarta (D-20).
        var request = new HttpRequestMessage(HttpMethod.Get, MariaUrl);
        request.Headers.TryAddWithoutValidation(Header, new string('x', 200));

        var response = await _client.SendAsync(request);

        Assert.True(Guid.TryParse(response.Headers.GetValues(Header).Single(), out _));
    }

    [Fact]
    public async Task RespuestaDeError_IncluyeElCorrelationIdRecibidoEnBodyYHeader()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/users/abc");
        request.Headers.Add(Header, "demo-error-456");

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("demo-error-456", response.Headers.GetValues(Header).Single());
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("demo-error-456", body.RootElement.GetProperty("correlationId").GetString());
    }
}
