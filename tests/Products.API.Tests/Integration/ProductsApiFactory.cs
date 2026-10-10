using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Products.API.Clients;

namespace Products.API.Tests.Integration;

/// <summary>
/// Levanta Products.API en memoria para los tests de integración: sin archivo de log y con la red
/// hacia Orders.API reemplazada por <see cref="FakeOrdersApi"/>, para no depender de que Orders esté levantado.
/// </summary>
public class ProductsApiFactory : WebApplicationFactory<Program>
{
    public FakeOrdersApi OrdersApi { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("LogFile:Enabled", "false");

        // Solo se reemplaza el último eslabón (la red): el OrdersClient, su DelegatingHandler
        // y el health check de Orders son los reales.
        builder.ConfigureTestServices(services =>
            services.AddHttpClient(nameof(IOrdersClient)).ConfigurePrimaryHttpMessageHandler(OrdersApi.CrearHandler));
    }
}
