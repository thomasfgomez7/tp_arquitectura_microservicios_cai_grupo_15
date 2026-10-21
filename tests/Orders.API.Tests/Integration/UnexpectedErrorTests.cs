using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Orders.API.Exceptions;
using Orders.API.Services;

namespace Orders.API.Tests.Integration;

public class UnexpectedErrorTests(OrdersApiFactory factory) : IClassFixture<OrdersApiFactory>
{
    private const string MensajeInterno = "Se cayó la conexión con la base de datos";

    [Fact]
    public async Task Get_ErrorInesperado_Devuelve500ConORD007SinStackTrace()
    {
        var client = ClienteConServicioQueFalla(incluirDetalle: false);

        var response = await client.GetAsync("/api/orders");

        var body = await ErrorContractAssert.IsErrorAsync(response, HttpStatusCode.InternalServerError, ErrorCodes.ORD_007,
            instance: "/api/orders", errorMessage: "Error interno al procesar la orden.");
        Assert.Equal("Internal Server Error", body.GetProperty("title").GetString());
        Assert.DoesNotContain("   at ", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Get_ErrorInesperadoSinDetalle_NoExponeElMensajeInterno()
    {
        var client = ClienteConServicioQueFalla(incluirDetalle: false);

        var response = await client.GetAsync("/api/orders");

        Assert.DoesNotContain(MensajeInterno, await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Get_ErrorInesperadoConDetalle_IncluyeElMensajeEnDetail()
    {
        var client = ClienteConServicioQueFalla(incluirDetalle: true);

        var response = await client.GetAsync("/api/orders");

        var body = await ErrorContractAssert.IsErrorAsync(response, HttpStatusCode.InternalServerError, ErrorCodes.ORD_007,
            instance: "/api/orders");
        Assert.Contains(MensajeInterno, body.GetProperty("detail").GetString());
    }

    private HttpClient ClienteConServicioQueFalla(bool incluirDetalle)
    {
        var service = Substitute.For<IOrderService>();
        service.GetAllAsync(Arg.Any<Guid?>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException(MensajeInterno));

        return factory
            .WithWebHostBuilder(b =>
            {
                b.UseSetting("ErrorHandling:IncludeExceptionDetails", incluirDetalle.ToString());
                b.ConfigureTestServices(s => s.AddScoped(_ => service));
            })
            .CreateClient();
    }
}
