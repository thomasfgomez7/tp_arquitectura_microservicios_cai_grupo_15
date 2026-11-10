using Dapper;

namespace Notifications.API.Repositories;

/// <summary>
/// Crea la tabla al arrancar el servicio, como el DatabaseInitializer del ToDo de la cátedra.
/// Notifications no tiene datos semilla: arranca vacío y las notificaciones sobreviven a los reinicios.
/// </summary>
public class DatabaseInitializer(SqliteConnectionFactory connectionFactory, ILogger<DatabaseInitializer> logger)
{
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        using var connection = connectionFactory.CreateConnection();
        await connection.ExecuteAsync(new CommandDefinition("""
            CREATE TABLE IF NOT EXISTS notifications (
                id          TEXT PRIMARY KEY,
                usuario_id  TEXT NOT NULL,
                mensaje     TEXT NOT NULL,
                tipo        TEXT NOT NULL,
                estado      TEXT NOT NULL,
                fecha_envio TEXT NOT NULL
            );
            CREATE INDEX IF NOT EXISTS ix_notifications_usuario_id ON notifications (usuario_id);
            """, cancellationToken: cancellationToken));

        logger.LogInformation("SQLite inicializado correctamente: {ConnectionString}", connectionFactory.ConnectionString);
    }
}
