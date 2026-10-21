using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Orders.API.Clients;

namespace Orders.API.Tests.Integration;

/// <summary>
/// Levanta Orders.API en memoria para los tests de integración: sin archivo de log y con la red hacia
/// Users.API y Products.API reemplazada por <see cref="FakeUsersApi"/> y <see cref="FakeProductsApi"/>,
/// para no depender de que estén levantados.
/// </summary>
public class OrdersApiFactory : WebApplicationFactory<Program>
{
    public FakeUsersApi UsersApi { get; } = new();

    public FakeProductsApi ProductsApi { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("LogFile:Enabled", "false");

        // Solo se reemplaza el último eslabón (la red): los clientes, su DelegatingHandler
        // y los health checks de Users y Products son los reales.
        builder.ConfigureTestServices(services =>
        {
            services.AddHttpClient(nameof(IUsersClient)).ConfigurePrimaryHttpMessageHandler(UsersApi.CrearHandler);
            services.AddHttpClient(nameof(IProductsClient)).ConfigurePrimaryHttpMessageHandler(ProductsApi.CrearHandler);
        });
    }
}
