using System.Net;

namespace Notifications.API.Tests.Integration;

public class SmokeTests(NotificationsApiFactory factory) : IClassFixture<NotificationsApiFactory>
{
    [Fact]
    public async Task Get_RutaInexistente_Devuelve404()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/ruta-inexistente");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
