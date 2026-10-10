using System.Net;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using NSubstitute;
using Products.API.Infrastructure;

namespace Products.API.Tests.Unit.Infrastructure;

public class DownstreamServiceHealthCheckTests
{
    private const string ClientName = "IOrdersClient";

    [Fact]
    public async Task CheckHealthAsync_ConsultaElLiveDelServicioConSuClienteConfigurado()
    {
        var red = new FakeHttpHandler(HttpStatusCode.OK);

        await CrearCheck(red).CheckHealthAsync(Contexto());

        Assert.Equal("http://orders.test/health/live", red.UltimoRequest!.RequestUri!.ToString());
    }

    [Fact]
    public async Task CheckHealthAsync_ServicioResponde200_DevuelveHealthy()
    {
        var resultado = await CrearCheck(new FakeHttpHandler(HttpStatusCode.OK)).CheckHealthAsync(Contexto());

        Assert.Equal(HealthStatus.Healthy, resultado.Status);
        Assert.Equal("Orders.API responde.", resultado.Description);
    }

    [Fact]
    public async Task CheckHealthAsync_ServicioRespondeUnError_DevuelveElEstadoDeFallaDelRegistro()
    {
        var resultado = await CrearCheck(new FakeHttpHandler(HttpStatusCode.ServiceUnavailable)).CheckHealthAsync(Contexto());

        Assert.Equal(HealthStatus.Degraded, resultado.Status);
        Assert.Equal("Orders.API no responde.", resultado.Description);
    }

    [Fact]
    public async Task CheckHealthAsync_ServicioNoResponde_DevuelveElEstadoDeFallaDelRegistro()
    {
        var red = new FakeHttpHandler(new HttpRequestException("Connection refused"));

        var resultado = await CrearCheck(red).CheckHealthAsync(Contexto());

        Assert.Equal(HealthStatus.Degraded, resultado.Status);
        Assert.IsType<HttpRequestException>(resultado.Exception);
    }

    private static DownstreamServiceHealthCheck CrearCheck(FakeHttpHandler red)
    {
        var factory = Substitute.For<IHttpClientFactory>();
        factory.CreateClient(ClientName).Returns(new HttpClient(red) { BaseAddress = new Uri("http://orders.test/") });

        return new DownstreamServiceHealthCheck(factory, ClientName, "Orders.API");
    }

    // El estado de falla lo define el registro (D-24: una dependencia caída degrada, no tumba el servicio).
    private static HealthCheckContext Contexto() => new()
    {
        Registration = new HealthCheckRegistration("Orders.API", Substitute.For<IHealthCheck>(), HealthStatus.Degraded, null)
    };
}
