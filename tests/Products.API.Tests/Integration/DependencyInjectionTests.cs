using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Products.API.Clients;
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

    [Fact]
    public void OrdersClient_SeRegistraComoTypedClientConLaUrlYElTimeoutDeOrders()
    {
        using var scope = factory.Services.CreateScope();

        Assert.IsType<OrdersClient>(scope.ServiceProvider.GetRequiredService<IOrdersClient>());

        // AddHttpClient<IOrdersClient, OrdersClient> registra un cliente con el nombre de la interfaz.
        var httpClient = scope.ServiceProvider.GetRequiredService<IHttpClientFactory>().CreateClient(nameof(IOrdersClient));
        Assert.Equal(new Uri("http://localhost:5003/"), httpClient.BaseAddress);
        Assert.Equal(TimeSpan.FromSeconds(5), httpClient.Timeout);
    }
}
