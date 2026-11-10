using System.Globalization;
using Dapper;
using Users.API.Models;

namespace Users.API.Repositories;

/// <summary>
/// Persistencia en SQLite con Dapper, siguiendo el ToDo de la cátedra (D-40).
/// SQLite solo tiene texto, enteros y reales: el Guid y la fecha se guardan como texto y el booleano
/// como 0/1, y se convierten acá, así el resto del servicio sigue trabajando con sus tipos de siempre.
/// La columna email es UNIQUE COLLATE NOCASE: el email es único sin distinguir mayúsculas, igual que antes.
/// </summary>
public class SqliteUserRepository(SqliteConnectionFactory connectionFactory) : IUserRepository
{
    private const string SelectColumns = """
        SELECT id, nombre, apellido, email, password_hash AS PasswordHash, fecha_registro AS FechaRegistro,
               activo, intentos_fallidos AS IntentosFallidos
        FROM users
        """;

    public async Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        using var connection = connectionFactory.CreateConnection();
        return await connection.ExecuteScalarAsync<bool>(new CommandDefinition(
            "SELECT EXISTS (SELECT 1 FROM users WHERE email = @Email)", new { Email = email },
            cancellationToken: cancellationToken));
    }

    public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default) =>
        await QuerySingleAsync($"{SelectColumns} WHERE email = @Value", email, cancellationToken);

    public async Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await QuerySingleAsync($"{SelectColumns} WHERE id = @Value", id.ToString(), cancellationToken);

    public async Task AddAsync(User user, CancellationToken cancellationToken = default)
    {
        // UserService ya verificó el email antes. Si dos registros iguales llegan al mismo tiempo,
        // el UNIQUE hace fallar el segundo: termina en un 500 (USR-006) en lugar de duplicar al usuario.
        using var connection = connectionFactory.CreateConnection();
        await connection.ExecuteAsync(new CommandDefinition("""
            INSERT INTO users (id, nombre, apellido, email, password_hash, fecha_registro, activo, intentos_fallidos)
            VALUES (@Id, @Nombre, @Apellido, @Email, @PasswordHash, @FechaRegistro, @Activo, @IntentosFallidos)
            """, ToParameters(user), cancellationToken: cancellationToken));
    }

    public async Task UpdateAsync(User user, CancellationToken cancellationToken = default)
    {
        using var connection = connectionFactory.CreateConnection();
        await connection.ExecuteAsync(new CommandDefinition("""
            UPDATE users
            SET nombre = @Nombre, apellido = @Apellido, email = @Email, password_hash = @PasswordHash,
                fecha_registro = @FechaRegistro, activo = @Activo, intentos_fallidos = @IntentosFallidos
            WHERE id = @Id
            """, ToParameters(user), cancellationToken: cancellationToken));
    }

    private async Task<User?> QuerySingleAsync(string sql, string value, CancellationToken cancellationToken)
    {
        using var connection = connectionFactory.CreateConnection();
        var row = await connection.QuerySingleOrDefaultAsync<UserRow>(new CommandDefinition(
            sql, new { Value = value }, cancellationToken: cancellationToken));

        return row is null ? null : ToModel(row);
    }

    private static object ToParameters(User user) => new
    {
        Id = user.Id.ToString(),
        user.Nombre,
        user.Apellido,
        user.Email,
        user.PasswordHash,
        FechaRegistro = user.FechaRegistro.ToString("O", CultureInfo.InvariantCulture),
        Activo = user.Activo ? 1 : 0,
        user.IntentosFallidos
    };

    private static User ToModel(UserRow row) => new()
    {
        Id = Guid.Parse(row.Id),
        Nombre = row.Nombre,
        Apellido = row.Apellido,
        Email = row.Email,
        PasswordHash = row.PasswordHash,
        FechaRegistro = DateTime.Parse(row.FechaRegistro, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind),
        Activo = row.Activo != 0,
        IntentosFallidos = (int)row.IntentosFallidos
    };

    // Una fila tal como la devuelve SQLite.
    private sealed class UserRow
    {
        public string Id { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public string Apellido { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string FechaRegistro { get; set; } = string.Empty;
        public long Activo { get; set; }
        public long IntentosFallidos { get; set; }
    }
}
