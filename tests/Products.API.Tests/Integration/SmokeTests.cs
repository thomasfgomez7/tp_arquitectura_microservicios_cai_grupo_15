using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Products.API.Tests.Integration;

public class SmokeTests(ProductsApiFactory factory) : IClassFixture<ProductsApiFactory>
{
    [Fact]
    public async Task Get_RutaInexistente_Devuelve404()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/ruta-inexistente");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
