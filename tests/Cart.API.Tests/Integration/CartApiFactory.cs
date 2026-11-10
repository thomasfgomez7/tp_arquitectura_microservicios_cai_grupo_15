using Cart.API.Clients;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;

namespace Cart.API.Tests.Integration;

/// <summary>
/// Levanta Cart.API en memoria para los tests de integración: sin archivo de log, con su propia base SQLite temporal y con la red hacia
/// Products.API reemplazada por <see cref="FakeProductsApi"/>, para no depender de que Products esté levantado.
/// </summary>
public class CartApiFactory : WebApplicationFactory<Program>
{
    // Cada factory arranca con una base nueva y los datos semilla, como antes con el repositorio en memoria:
    // los tests no se pisan entre sí ni tocan la base de la app (src/Cart.API/cart.db).
    private readonly string _databasePath = Path.Combine(Path.GetTempPath(), $"cart-tests-{Guid.NewGuid():N}.db");

    public FakeProductsApi ProductsApi { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("LogFile:Enabled", "false");
        builder.UseSetting("ConnectionStrings:DefaultConnection", $"Data Source={_databasePath}");

        // Solo se reemplaza el último eslabón (la red): el ProductsClient, su DelegatingHandler
        // y el health check de Products son los reales.
        builder.ConfigureTestServices(services =>
            services.AddHttpClient(nameof(IProductsClient)).ConfigurePrimaryHttpMessageHandler(ProductsApi.CrearHandler));
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
