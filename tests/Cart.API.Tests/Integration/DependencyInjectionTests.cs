using Cart.API.Clients;
using Cart.API.Services;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace Cart.API.Tests.Integration;

/// <summary>
/// Verifica la configuración REAL del contenedor, sin los reemplazos de CartApiFactory:
/// los tests unitarios crean las clases a mano y no detectan un registro faltante.
/// </summary>
public class DependencyInjectionTests : IDisposable
{
    private readonly WebApplicationFactory<Program> _factory =
        new WebApplicationFactory<Program>().WithWebHostBuilder(b => b.UseSetting("LogFile:Enabled", "false"));

    [Fact]
    public void CartService_ConLaConfiguracionDeLaApp_SeResuelveConTodasSusDependencias()
    {
        using var scope = _factory.Services.CreateScope();

        Assert.IsType<CartService>(scope.ServiceProvider.GetRequiredService<ICartService>());
    }

    [Fact]
    public void ProductsClient_SeRegistraComoTypedClientConLaUrlDeProducts()
    {
        using var scope = _factory.Services.CreateScope();

        Assert.IsType<ProductsClient>(scope.ServiceProvider.GetRequiredService<IProductsClient>());

        // AddHttpClient<IProductsClient, ProductsClient> registra un cliente con el nombre de la interfaz.
        var httpClient = scope.ServiceProvider.GetRequiredService<IHttpClientFactory>().CreateClient(nameof(IProductsClient));
        Assert.Equal(new Uri("http://localhost:5001/"), httpClient.BaseAddress);
    }

    public void Dispose() => _factory.Dispose();
}
