using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Cart.API.Tests.Integration;

public class SmokeTests(CartApiFactory factory) : IClassFixture<CartApiFactory>
{
    [Fact]
    public async Task Get_RutaInexistente_Devuelve404()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/ruta-inexistente");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
