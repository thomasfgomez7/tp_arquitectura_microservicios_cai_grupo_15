using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Cart.API.Exceptions;
using Cart.API.Services;

namespace Cart.API.Tests.Integration;

public class UnexpectedErrorTests(CartApiFactory factory) : IClassFixture<CartApiFactory>
{
    private const string MensajeInterno = "Se cayó la conexión con la base de datos";

    [Fact]
    public async Task Get_ErrorInesperado_Devuelve500ConCRT005SinStackTrace()
    {
        var client = ClienteConServicioQueFalla(incluirDetalle: false);

        var response = await client.GetAsync("/api/cart/a1b2c3d4-0000-0000-0000-111122223333");

        var body = await ErrorContractAssert.IsErrorAsync(response, HttpStatusCode.InternalServerError, ErrorCodes.CRT_005,
            instance: "/api/cart/a1b2c3d4-0000-0000-0000-111122223333", errorMessage: "Error interno al procesar el carrito.");
        Assert.Equal("Internal Server Error", body.GetProperty("title").GetString());
        Assert.DoesNotContain("   at ", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Get_ErrorInesperadoSinDetalle_NoExponeElMensajeInterno()
    {
        var client = ClienteConServicioQueFalla(incluirDetalle: false);

        var response = await client.GetAsync("/api/cart/a1b2c3d4-0000-0000-0000-111122223333");

        Assert.DoesNotContain(MensajeInterno, await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Get_ErrorInesperadoConDetalle_IncluyeElMensajeEnDetail()
    {
        var client = ClienteConServicioQueFalla(incluirDetalle: true);

        var response = await client.GetAsync("/api/cart/a1b2c3d4-0000-0000-0000-111122223333");

        var body = await ErrorContractAssert.IsErrorAsync(response, HttpStatusCode.InternalServerError, ErrorCodes.CRT_005,
            instance: "/api/cart/a1b2c3d4-0000-0000-0000-111122223333");
        Assert.Contains(MensajeInterno, body.GetProperty("detail").GetString());
    }

    private HttpClient ClienteConServicioQueFalla(bool incluirDetalle)
    {
        var service = Substitute.For<ICartService>();
        service.GetAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
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
