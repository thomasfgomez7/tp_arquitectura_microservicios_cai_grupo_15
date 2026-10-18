using System.Net;
using System.Text.Json;
using Orders.API.Repositories;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace Orders.API.Tests.Integration;

/// <summary>
/// Requisitos de la sección 5.4 del enunciado, con Users.API y Products.API en /health/ready (Etapa 9).
/// </summary>
public class HealthCheckTests(OrdersApiFactory factory) : IClassFixture<OrdersApiFactory>
{
    [Theory]
    [InlineData("/health")]
    [InlineData("/health/ready")]
    [InlineData("/health/live")]
    public async Task HealthCheck_ServicioSano_Devuelve200ConJsonHealthy(string url)
    {
        var response = await factory.CreateClient().GetAsync(url);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("Healthy", body.RootElement.GetProperty("status").GetString());
        Assert.Equal("Orders.API", body.RootElement.GetProperty("servicio").GetString());
        Assert.NotEmpty(body.RootElement.GetProperty("checks").EnumerateArray());
    }

    [Theory]
    [InlineData("Users.API")]
    [InlineData("Products.API")]
    public async Task Ready_IncluyeElChequeoDeCadaServicioDelQueDepende(string servicio)
    {
        var response = await factory.CreateClient().GetAsync("/health/ready");

        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var check = Assert.Single(body.RootElement.GetProperty("checks").EnumerateArray(),
            c => c.GetProperty("name").GetString() == servicio);
        Assert.Equal("Healthy", check.GetProperty("status").GetString());
    }

    [Theory]
    [InlineData("Users.API")]
    [InlineData("Products.API")]
    public async Task Ready_UnServicioCaido_Devuelve200DegradedYLiveSigueHealthy(string servicio)
    {
        // D-24: sin Users o Products falla la creación de órdenes, pero se pueden consultar y cambiar de estado.
        using var conServicioCaido = new OrdersApiFactory();
        if (servicio == "Users.API")
        {
            conServicioCaido.UsersApi.Caido = true;
        }
        else
        {
            conServicioCaido.ProductsApi.Caido = true;
        }
        var client = conServicioCaido.CreateClient();

        var ready = await client.GetAsync("/health/ready");
        var live = await client.GetAsync("/health/live");

        Assert.Equal(HttpStatusCode.OK, ready.StatusCode);
        using var body = JsonDocument.Parse(await ready.Content.ReadAsStringAsync());
        Assert.Equal("Degraded", body.RootElement.GetProperty("status").GetString());
        var checks = body.RootElement.GetProperty("checks").EnumerateArray()
            .ToDictionary(c => c.GetProperty("name").GetString()!, c => c.GetProperty("status").GetString());
        Assert.Equal("Degraded", checks[servicio]);
        Assert.Equal("Healthy", checks[servicio == "Users.API" ? "Products.API" : "Users.API"]);
        using var liveBody = JsonDocument.Parse(await live.Content.ReadAsStringAsync());
        Assert.Equal("Healthy", liveBody.RootElement.GetProperty("status").GetString());
    }

    [Fact]
    public async Task Ready_PersistenciaCaida_Devuelve503YLiveSigueEn200()
    {
        var repository = Substitute.For<IOrderRepository>();
        repository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).ThrowsAsync(new InvalidOperationException("Sin conexión"));
        var client = factory.WithWebHostBuilder(b => b.ConfigureTestServices(s => s.AddSingleton(repository))).CreateClient();

        var ready = await client.GetAsync("/health/ready");
        var live = await client.GetAsync("/health/live");

        Assert.Equal(HttpStatusCode.ServiceUnavailable, ready.StatusCode);
        Assert.Equal(HttpStatusCode.OK, live.StatusCode);
    }
}
