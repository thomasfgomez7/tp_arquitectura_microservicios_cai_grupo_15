using Dapper;
using Microsoft.AspNetCore.Identity;
using Users.API.Models;

namespace Users.API.Repositories;

/// <summary>
/// Crea la tabla al arrancar el servicio, como el DatabaseInitializer del ToDo de la cátedra.
/// La primera vez carga los usuarios de la demo; después los datos sobreviven a los reinicios
/// (por ejemplo, un usuario bloqueado en vivo sigue bloqueado).
/// </summary>
public class DatabaseInitializer(
    SqliteConnectionFactory connectionFactory,
    IPasswordHasher<User> passwordHasher,
    ILogger<DatabaseInitializer> logger)
{
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        using var connection = connectionFactory.CreateConnection();
        await connection.ExecuteAsync(new CommandDefinition("""
            CREATE TABLE IF NOT EXISTS users (
                id                TEXT    PRIMARY KEY,
                nombre            TEXT    NOT NULL,
                apellido          TEXT    NOT NULL,
                email             TEXT    NOT NULL UNIQUE COLLATE NOCASE,
                password_hash     TEXT    NOT NULL,
                fecha_registro    TEXT    NOT NULL,
                activo            INTEGER NOT NULL,
                intentos_fallidos INTEGER NOT NULL
            );
            """, cancellationToken: cancellationToken));

        var count = await connection.ExecuteScalarAsync<long>(new CommandDefinition(
            "SELECT COUNT(*) FROM users", cancellationToken: cancellationToken));
        if (count == 0)
        {
            // Se usa el repositorio concreto, no la interfaz: un test que la reemplaza no afecta la carga inicial.
            var repository = new SqliteUserRepository(connectionFactory);
            foreach (var user in UserSeedData.Create(passwordHasher))
            {
                await repository.AddAsync(user, cancellationToken);
            }
        }

        logger.LogInformation("SQLite inicializado correctamente: {ConnectionString}", connectionFactory.ConnectionString);
    }
}
