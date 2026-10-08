using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Products.API.Repositories;

namespace Products.API.Tests.Integration;

/// <summary>
/// Requisitos de la sección 5.4 del enunciado: GET /health, /health/ready y /health/live
/// con respuesta JSON y estado Healthy, Degraded o Unhealthy.
/// </summary>
public class HealthCheckTests(ProductsApiFactory factory) : IClassFixture<ProductsApiFactory>
{
    [Theory]
    [InlineData("/health")]
    [InlineData("/health/ready")]
    [InlineData("/health/live")]
    public async Task HealthCheck_ServicioSano_Devuelve200ConJsonHealthy(string url)
    {
        var response = await factory.CreateClient().GetAsync(url);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("Healthy", body.RootElement.GetProperty("status").GetString());
        Assert.Equal("Products.API", body.RootElement.GetProperty("servicio").GetString());
        Assert.True(body.RootElement.GetProperty("totalDurationMs").GetDouble() >= 0);
        Assert.NotEmpty(body.RootElement.GetProperty("checks").EnumerateArray());
    }

    [Fact]
    public async Task Ready_PersistenciaCaida_Devuelve503Unhealthy()
    {
        var client = ClienteConRepositorioCaido();

        var response = await client.GetAsync("/health/ready");

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("Unhealthy", body.RootElement.GetProperty("status").GetString());
        var check = Assert.Single(body.RootElement.GetProperty("checks").EnumerateArray(),
            c => c.GetProperty("name").GetString() == "persistencia");
        Assert.Equal("Unhealthy", check.GetProperty("status").GetString());
    }

    [Fact]
    public async Task Live_PersistenciaCaida_SigueDevolviendo200()
    {
        // /health/live solo responde si el proceso está vivo: no depende de la persistencia.
        var client = ClienteConRepositorioCaido();

        var response = await client.GetAsync("/health/live");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private HttpClient ClienteConRepositorioCaido()
    {
        var repository = Substitute.For<IProductRepository>();
        repository.GetAllAsync(Arg.Any<CancellationToken>()).ThrowsAsync(new InvalidOperationException("Sin conexión"));

        return factory
            .WithWebHostBuilder(b => b.ConfigureTestServices(s => s.AddSingleton(repository)))
            .CreateClient();
    }
}
