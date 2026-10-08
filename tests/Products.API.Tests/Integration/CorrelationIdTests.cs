using System.Net;
using System.Text.Json;

namespace Products.API.Tests.Integration;

public class CorrelationIdTests(ProductsApiFactory factory) : IClassFixture<ProductsApiFactory>
{
    private const string Header = "X-Correlation-Id";

    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Request_SinHeader_GeneraUnCorrelationIdEnLaRespuesta()
    {
        var response = await _client.GetAsync("/api/products");

        var correlationId = response.Headers.GetValues(Header).Single();
        Assert.True(Guid.TryParse(correlationId, out _));
    }

    [Fact]
    public async Task Request_SinHeader_GeneraUnIdDistintoPorRequest()
    {
        var primero = await _client.GetAsync("/api/products");
        var segundo = await _client.GetAsync("/api/products");

        Assert.NotEqual(primero.Headers.GetValues(Header).Single(), segundo.Headers.GetValues(Header).Single());
    }

    [Fact]
    public async Task Request_ConHeader_DevuelveElMismoCorrelationId()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/products");
        request.Headers.Add(Header, "demo-123");

        var response = await _client.SendAsync(request);

        Assert.Equal("demo-123", response.Headers.GetValues(Header).Single());
    }

    [Fact]
    public async Task Request_ConHeaderInvalido_LoReemplazaPorUnoNuevo()
    {
        // Un valor demasiado largo o con caracteres raros podría ensuciar los logs: se descarta.
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/products");
        request.Headers.TryAddWithoutValidation(Header, new string('x', 200));

        var response = await _client.SendAsync(request);

        Assert.True(Guid.TryParse(response.Headers.GetValues(Header).Single(), out _));
    }

    [Fact]
    public async Task RespuestaDeError_IncluyeElCorrelationIdRecibidoEnBodyYHeader()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/products/99");
        request.Headers.Add(Header, "demo-error-456");

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("demo-error-456", response.Headers.GetValues(Header).Single());
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("demo-error-456", body.RootElement.GetProperty("correlationId").GetString());
    }
}
