using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Products.API.Exceptions;
using Products.API.Services;

namespace Products.API.Tests.Integration;

public class UnexpectedErrorTests(ProductsApiFactory factory) : IClassFixture<ProductsApiFactory>
{
    private const string MensajeInterno = "Se cayó la conexión con la base de datos";

    [Fact]
    public async Task GetAll_ErrorInesperado_Devuelve500ConPRD005SinStackTrace()
    {
        var client = ClienteConServicioQueFalla(incluirDetalle: false);

        var response = await client.GetAsync("/api/products");

        var body = await ErrorContractAssert.IsErrorAsync(response, HttpStatusCode.InternalServerError, ErrorCodes.PRD_005,
            instance: "/api/products", errorMessage: "Error interno al procesar el producto.");
        Assert.Equal("Internal Server Error", body.GetProperty("title").GetString());
        Assert.DoesNotContain("   at ", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task GetAll_ErrorInesperadoSinDetalle_NoExponeElMensajeInterno()
    {
        var client = ClienteConServicioQueFalla(incluirDetalle: false);

        var response = await client.GetAsync("/api/products");

        Assert.DoesNotContain(MensajeInterno, await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task GetAll_ErrorInesperadoConDetalle_IncluyeElMensajeEnDetail()
    {
        var client = ClienteConServicioQueFalla(incluirDetalle: true);

        var response = await client.GetAsync("/api/products");

        var body = await ErrorContractAssert.IsErrorAsync(response, HttpStatusCode.InternalServerError, ErrorCodes.PRD_005,
            instance: "/api/products");
        Assert.Contains(MensajeInterno, body.GetProperty("detail").GetString());
    }

    private HttpClient ClienteConServicioQueFalla(bool incluirDetalle)
    {
        var service = Substitute.For<IProductService>();
        service.GetAllAsync(Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
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
