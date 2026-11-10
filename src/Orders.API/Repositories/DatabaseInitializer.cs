using Dapper;

namespace Orders.API.Repositories;

/// <summary>
/// Crea las tablas al arrancar el servicio, como el DatabaseInitializer del ToDo de la cátedra.
/// La primera vez carga la orden semilla (la que muestra PRD-004); después los datos sobreviven a los reinicios.
/// </summary>
public class DatabaseInitializer(SqliteConnectionFactory connectionFactory, ILogger<DatabaseInitializer> logger)
{
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        using var connection = connectionFactory.CreateConnection();
        // "posicion" conserva el orden de los items, como la lista en memoria. El índice por producto
        // acelera GET /api/orders?productoId=, que Products consulta antes de borrar (D-07).
        await connection.ExecuteAsync(new CommandDefinition("""
            CREATE TABLE IF NOT EXISTS orders (
                id                  TEXT PRIMARY KEY,
                usuario_id          TEXT NOT NULL,
                total               TEXT NOT NULL,
                estado              TEXT NOT NULL,
                fecha_creacion      TEXT NOT NULL,
                fecha_actualizacion TEXT NOT NULL
            );
            CREATE TABLE IF NOT EXISTS order_items (
                order_id        TEXT    NOT NULL REFERENCES orders (id),
                producto_id     TEXT    NOT NULL,
                cantidad        INTEGER NOT NULL,
                precio_unitario TEXT    NOT NULL,
                posicion        INTEGER NOT NULL,
                PRIMARY KEY (order_id, producto_id)
            );
            CREATE INDEX IF NOT EXISTS ix_order_items_producto_id ON order_items (producto_id);
            """, cancellationToken: cancellationToken));

        var count = await connection.ExecuteScalarAsync<long>(new CommandDefinition(
            "SELECT COUNT(*) FROM orders", cancellationToken: cancellationToken));
        if (count == 0)
        {
            // Se usa el repositorio concreto, no la interfaz: un test que la reemplaza no afecta la carga inicial.
            var repository = new SqliteOrderRepository(connectionFactory);
            foreach (var order in OrderSeedData.Create())
            {
                await repository.AddAsync(order, cancellationToken);
            }
        }

        logger.LogInformation("SQLite inicializado correctamente: {ConnectionString}", connectionFactory.ConnectionString);
    }
}
