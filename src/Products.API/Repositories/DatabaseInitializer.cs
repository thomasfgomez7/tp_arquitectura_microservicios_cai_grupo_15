using Dapper;

namespace Products.API.Repositories;

/// <summary>
/// Crea la tabla al arrancar el servicio, como el DatabaseInitializer del ToDo de la cátedra.
/// La primera vez carga los datos semilla; después los datos sobreviven a los reinicios.
/// </summary>
public class DatabaseInitializer(SqliteConnectionFactory connectionFactory, ILogger<DatabaseInitializer> logger)
{
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        using var connection = connectionFactory.CreateConnection();
        await connection.ExecuteAsync(new CommandDefinition("""
            CREATE TABLE IF NOT EXISTS products (
                id             TEXT    PRIMARY KEY,
                nombre         TEXT    NOT NULL,
                descripcion    TEXT,
                precio         TEXT    NOT NULL,
                stock          INTEGER NOT NULL,
                categoria      TEXT    NOT NULL,
                fecha_creacion TEXT    NOT NULL
            );
            """, cancellationToken: cancellationToken));

        var count = await connection.ExecuteScalarAsync<long>(new CommandDefinition(
            "SELECT COUNT(*) FROM products", cancellationToken: cancellationToken));
        if (count == 0)
        {
            // Se usa el repositorio concreto, no la interfaz: un test que la reemplaza no afecta la carga inicial.
            var repository = new SqliteProductRepository(connectionFactory);
            foreach (var product in ProductSeedData.Create())
            {
                await repository.AddAsync(product, cancellationToken);
            }
        }

        logger.LogInformation("SQLite inicializado correctamente: {ConnectionString}", connectionFactory.ConnectionString);
    }
}
