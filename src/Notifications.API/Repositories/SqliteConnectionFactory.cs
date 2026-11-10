using Microsoft.Data.Sqlite;

namespace Notifications.API.Repositories;

/// <summary>
/// Crea las conexiones a la base SQLite del servicio, con la cadena "ConnectionStrings:DefaultConnection"
/// (como el ToDo de la cátedra). Una ruta relativa se resuelve contra la carpeta del proyecto, así la base
/// queda en src/Notifications.API/ sin importar desde dónde se levante el servicio.
/// </summary>
public class SqliteConnectionFactory
{
    public SqliteConnectionFactory(IConfiguration configuration, IHostEnvironment environment)
    {
        var builder = new SqliteConnectionStringBuilder(
            configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Falta la configuración 'ConnectionStrings:DefaultConnection'."));

        if (!Path.IsPathRooted(builder.DataSource))
        {
            builder.DataSource = Path.Combine(environment.ContentRootPath, builder.DataSource);
        }

        ConnectionString = builder.ToString();
    }

    public string ConnectionString { get; }

    // Una conexión por operación, como en el ToDo: Microsoft.Data.Sqlite las reutiliza con su pool.
    public SqliteConnection CreateConnection() => new(ConnectionString);
}
