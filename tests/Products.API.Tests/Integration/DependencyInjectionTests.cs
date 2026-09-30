using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Products.API.Services;

namespace Products.API.Tests.Integration;

public class DependencyInjectionTests(ProductsApiFactory factory) : IClassFixture<ProductsApiFactory>
{
    [Fact]
    public void ProductService_ConLaConfiguracionDeLaApp_SeResuelveConTodasSusDependencias()
    {
        using var scope = factory.Services.CreateScope();

        var service = scope.ServiceProvider.GetRequiredService<IProductService>();

        Assert.IsType<ProductService>(service);
    }
}
