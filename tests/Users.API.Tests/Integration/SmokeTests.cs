using System.Net;

namespace Users.API.Tests.Integration;

public class SmokeTests(UsersApiFactory factory) : IClassFixture<UsersApiFactory>
{
    [Fact]
    public async Task Get_RutaInexistente_Devuelve404()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/ruta-inexistente");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
