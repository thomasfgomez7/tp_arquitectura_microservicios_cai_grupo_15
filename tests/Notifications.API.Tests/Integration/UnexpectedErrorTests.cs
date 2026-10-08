using System.Net;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Notifications.API.Exceptions;
using Notifications.API.Services;

namespace Notifications.API.Tests.Integration;

/// <summary>
/// Errores inesperados (NTF-004): sin stack trace y con el detalle según el entorno (D-18).
/// </summary>
public class UnexpectedErrorTests(NotificationsApiFactory factory) : IClassFixture<NotificationsApiFactory>
{
    private const string MensajeInterno = "Se cayó la conexión con la base de datos";
    private const string Url = "/api/notifications/a1b2c3d4-0000-0000-0000-111122223333";

    [Fact]
    public async Task Get_ErrorInesperado_Devuelve500ConNTF004SinStackTrace()
    {
        var client = ClienteConServicioQueFalla(incluirDetalle: false);

        var response = await client.GetAsync(Url);

        var body = await ErrorContractAssert.IsErrorAsync(response, HttpStatusCode.InternalServerError, ErrorCodes.NTF_004,
            instance: Url, errorMessage: "Error interno al procesar la notificación.");
        Assert.Equal("Internal Server Error", body.GetProperty("title").GetString());
        Assert.DoesNotContain("   at ", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Get_ErrorInesperadoSinDetalle_NoExponeElMensajeInterno()
    {
        var client = ClienteConServicioQueFalla(incluirDetalle: false);

        var response = await client.GetAsync(Url);

        Assert.DoesNotContain(MensajeInterno, await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Get_ErrorInesperadoConDetalle_IncluyeElMensajeEnDetail()
    {
        var client = ClienteConServicioQueFalla(incluirDetalle: true);

        var response = await client.GetAsync(Url);

        var body = await ErrorContractAssert.IsErrorAsync(response, HttpStatusCode.InternalServerError, ErrorCodes.NTF_004,
            instance: Url);
        Assert.Contains(MensajeInterno, body.GetProperty("detail").GetString());
    }

    private HttpClient ClienteConServicioQueFalla(bool incluirDetalle)
    {
        var service = Substitute.For<INotificationService>();
        service.GetByUserIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
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
