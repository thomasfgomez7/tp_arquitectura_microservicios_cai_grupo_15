using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using Orders.API.Clients;

namespace Orders.API.Tests.Integration;

/// <summary>
/// Levanta Orders.API en memoria para los tests de integración: sin archivo de log, con su propia base SQLite temporal y con la red hacia
/// Users.API y Products.API reemplazada por <see cref="FakeUsersApi"/> y <see cref="FakeProductsApi"/>,
/// para no depender de que estén levantados.
/// </summary>
public class OrdersApiFactory : WebApplicationFactory<Program>
{
    // Cada factory arranca con una base nueva y los datos semilla, como antes con el repositorio en memoria:
    // los tests no se pisan entre sí ni tocan la base de la app (src/Orders.API/orders.db).
    private readonly string _databasePath = Path.Combine(Path.GetTempPath(), $"orders-tests-{Guid.NewGuid():N}.db");

    public FakeUsersApi UsersApi { get; } = new();

    public FakeProductsApi ProductsApi { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("LogFile:Enabled", "false");
        builder.UseSetting("ConnectionStrings:DefaultConnection", $"Data Source={_databasePath}");

        // Solo se reemplaza el último eslabón (la red): los clientes, su DelegatingHandler
        // y los health checks de Users y Products son los reales.
        builder.ConfigureTestServices(services =>
        {
            services.AddHttpClient(nameof(IUsersClient)).ConfigurePrimaryHttpMessageHandler(UsersApi.CrearHandler);
            services.AddHttpClient(nameof(IProductsClient)).ConfigurePrimaryHttpMessageHandler(ProductsApi.CrearHandler);
        });
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
