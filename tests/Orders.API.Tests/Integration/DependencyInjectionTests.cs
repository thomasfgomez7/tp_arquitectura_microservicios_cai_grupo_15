using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Orders.API.Clients;
using Orders.API.Services;

namespace Orders.API.Tests.Integration;

/// <summary>
/// Verifica la configuración REAL del contenedor, sin los reemplazos de OrdersApiFactory:
/// los tests unitarios crean las clases a mano y no detectan un registro faltante.
/// </summary>
public class DependencyInjectionTests : IDisposable
{
    private readonly WebApplicationFactory<Program> _factory =
        new WebApplicationFactory<Program>().WithWebHostBuilder(b => b.UseSetting("LogFile:Enabled", "false"));

    [Fact]
    public void OrderService_ConLaConfiguracionDeLaApp_SeResuelveConTodasSusDependencias()
    {
        using var scope = _factory.Services.CreateScope();

        Assert.IsType<OrderService>(scope.ServiceProvider.GetRequiredService<IOrderService>());
    }

    [Fact]
    public void UsersClient_SeRegistraComoTypedClientConLaUrlDeUsersYTimeout()
    {
        using var scope = _factory.Services.CreateScope();

        Assert.IsType<UsersClient>(scope.ServiceProvider.GetRequiredService<IUsersClient>());

        // AddHttpClient<IUsersClient, UsersClient> registra un cliente con el nombre de la interfaz.
        var httpClient = scope.ServiceProvider.GetRequiredService<IHttpClientFactory>().CreateClient(nameof(IUsersClient));
        Assert.Equal(new Uri("http://localhost:5002/"), httpClient.BaseAddress);
        Assert.Equal(TimeSpan.FromSeconds(5), httpClient.Timeout);
    }

    [Fact]
    public void ProductsClient_SeRegistraComoTypedClientConLaUrlDeProductsYTimeout()
    {
        using var scope = _factory.Services.CreateScope();

        Assert.IsType<ProductsClient>(scope.ServiceProvider.GetRequiredService<IProductsClient>());

        var httpClient = scope.ServiceProvider.GetRequiredService<IHttpClientFactory>().CreateClient(nameof(IProductsClient));
        Assert.Equal(new Uri("http://localhost:5001/"), httpClient.BaseAddress);
        Assert.Equal(TimeSpan.FromSeconds(5), httpClient.Timeout);
    }

    public void Dispose() => _factory.Dispose();
}
