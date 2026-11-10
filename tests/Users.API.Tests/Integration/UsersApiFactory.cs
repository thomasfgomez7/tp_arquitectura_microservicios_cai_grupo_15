using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;

namespace Users.API.Tests.Integration;

/// <summary>
/// Levanta Users.API en memoria para los tests de integración, sin escribir el archivo de log
/// y con su propia base SQLite temporal.
/// </summary>
public class UsersApiFactory : WebApplicationFactory<Program>
{
    // Cada factory arranca con una base nueva y los datos semilla, como antes con el repositorio en memoria:
    // los tests no se pisan entre sí ni tocan la base de la app (src/Users.API/users.db).
    private readonly string _databasePath = Path.Combine(Path.GetTempPath(), $"users-tests-{Guid.NewGuid():N}.db");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("LogFile:Enabled", "false");
        builder.UseSetting("ConnectionStrings:DefaultConnection", $"Data Source={_databasePath}");
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
