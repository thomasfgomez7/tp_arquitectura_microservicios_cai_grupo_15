using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using Products.API.Clients;

namespace Products.API.Tests.Integration;

/// <summary>
/// Levanta Products.API en memoria para los tests de integración: sin archivo de log, con su propia base
/// SQLite temporal y con la red hacia Orders.API reemplazada por <see cref="FakeOrdersApi"/>,
/// para no depender de que Orders esté levantado.
/// </summary>
public class ProductsApiFactory : WebApplicationFactory<Program>
{
    // Cada factory arranca con una base nueva y los datos semilla, como antes con el repositorio en memoria:
    // los tests no se pisan entre sí ni tocan la base de la app (src/Products.API/products.db).
    private readonly string _databasePath = Path.Combine(Path.GetTempPath(), $"products-tests-{Guid.NewGuid():N}.db");

    public FakeOrdersApi OrdersApi { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("LogFile:Enabled", "false");
        builder.UseSetting("ConnectionStrings:DefaultConnection", $"Data Source={_databasePath}");

        // Solo se reemplaza el último eslabón (la red): el OrdersClient, su DelegatingHandler
        // y el health check de Orders son los reales.
        builder.ConfigureTestServices(services =>
            services.AddHttpClient(nameof(IOrdersClient)).ConfigurePrimaryHttpMessageHandler(OrdersApi.CrearHandler));
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        // El pool de conexiones mantiene el archivo abierto: se libera antes de borrarlo.
        SqliteConnection.ClearAllPools();
        try
        {
            File.Delete(_databasePath);
        }
        catch (IOException)
        {
            // Es un archivo temporal: si sigue en uso, lo limpia el sistema.
        }
    }
}
