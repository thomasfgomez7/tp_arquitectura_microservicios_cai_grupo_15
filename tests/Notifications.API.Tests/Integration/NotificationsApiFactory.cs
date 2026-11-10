using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using Notifications.API.Clients;

namespace Notifications.API.Tests.Integration;

/// <summary>
/// Levanta Notifications.API en memoria para los tests de integración: sin archivo de log, con su propia base SQLite temporal y con la red hacia
/// Users.API reemplazada por <see cref="FakeUsersApi"/>, para no depender de que Users esté levantado.
/// </summary>
public class NotificationsApiFactory : WebApplicationFactory<Program>
{
    // Cada factory arranca con una base nueva, como antes con el repositorio en memoria:
    // los tests no se pisan entre sí ni tocan la base de la app (src/Notifications.API/notifications.db).
    private readonly string _databasePath = Path.Combine(Path.GetTempPath(), $"notifications-tests-{Guid.NewGuid():N}.db");

    public FakeUsersApi UsersApi { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("LogFile:Enabled", "false");
        builder.UseSetting("ConnectionStrings:DefaultConnection", $"Data Source={_databasePath}");

        // Solo se reemplaza el último eslabón (la red): el UsersClient, su DelegatingHandler
        // y el health check de Users son los reales.
        builder.ConfigureTestServices(services =>
            services.AddHttpClient(nameof(IUsersClient)).ConfigurePrimaryHttpMessageHandler(UsersApi.CrearHandler));
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
