using Cart.API.Clients;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace Cart.API.Tests.Integration;

/// <summary>
/// Levanta Cart.API en memoria para los tests de integración: sin archivo de log y con la red hacia
/// Products.API reemplazada por <see cref="FakeProductsApi"/>, para no depender de que Products esté levantado.
/// </summary>
public class CartApiFactory : WebApplicationFactory<Program>
{
    public FakeProductsApi ProductsApi { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("LogFile:Enabled", "false");

        // Solo se reemplaza el último eslabón (la red): el ProductsClient, su DelegatingHandler
        // y el health check de Products son los reales.
        builder.ConfigureTestServices(services =>
            services.AddHttpClient(nameof(IProductsClient)).ConfigurePrimaryHttpMessageHandler(ProductsApi.CrearHandler));
    }
}
