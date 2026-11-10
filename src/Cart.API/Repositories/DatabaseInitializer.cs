using Dapper;

namespace Cart.API.Repositories;

/// <summary>
/// Crea las tablas al arrancar el servicio, como el DatabaseInitializer del ToDo de la cátedra.
/// La primera vez carga el carrito de la demo; después los datos sobreviven a los reinicios.
/// </summary>
public class DatabaseInitializer(SqliteConnectionFactory connectionFactory, ILogger<DatabaseInitializer> logger)
{
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        using var connection = connectionFactory.CreateConnection();
        // "posicion" conserva el orden en que se agregaron los productos, como la lista en memoria.
        await connection.ExecuteAsync(new CommandDefinition("""
            CREATE TABLE IF NOT EXISTS carts (
                usuario_id          TEXT PRIMARY KEY,
                fecha_actualizacion TEXT NOT NULL
            );
            CREATE TABLE IF NOT EXISTS cart_items (
                usuario_id  TEXT    NOT NULL REFERENCES carts (usuario_id),
                producto_id TEXT    NOT NULL,
                cantidad    INTEGER NOT NULL,
                posicion    INTEGER NOT NULL,
                PRIMARY KEY (usuario_id, producto_id)
            );
            """, cancellationToken: cancellationToken));

        var count = await connection.ExecuteScalarAsync<long>(new CommandDefinition(
            "SELECT COUNT(*) FROM carts", cancellationToken: cancellationToken));
        if (count == 0)
        {
            // Se usa el repositorio concreto, no la interfaz: un test que la reemplaza no afecta la carga inicial.
            var repository = new SqliteCartRepository(connectionFactory);
            foreach (var cart in CartSeedData.Create())
            {
                await repository.SaveAsync(cart, cancellationToken);
            }
        }

        logger.LogInformation("SQLite inicializado correctamente: {ConnectionString}", connectionFactory.ConnectionString);
    }
}
