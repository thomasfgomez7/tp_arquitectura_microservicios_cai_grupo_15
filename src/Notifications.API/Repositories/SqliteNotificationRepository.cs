using System.Globalization;
using Dapper;
using Notifications.API.Models;

namespace Notifications.API.Repositories;

/// <summary>
/// Persistencia en SQLite con Dapper, siguiendo el ToDo de la cátedra (D-40).
/// SQLite solo tiene texto, enteros y reales: los Guid, los enums y la fecha se guardan como texto
/// y se convierten acá, así el resto del servicio sigue trabajando con sus tipos de siempre.
/// </summary>
public class SqliteNotificationRepository(SqliteConnectionFactory connectionFactory) : INotificationRepository
{
    public async Task AddAsync(Notification notification, CancellationToken cancellationToken = default)
    {
        using var connection = connectionFactory.CreateConnection();
        await connection.ExecuteAsync(new CommandDefinition("""
            INSERT INTO notifications (id, usuario_id, mensaje, tipo, estado, fecha_envio)
            VALUES (@Id, @UsuarioId, @Mensaje, @Tipo, @Estado, @FechaEnvio)
            """,
            new
            {
                Id = notification.Id.ToString(),
                UsuarioId = notification.UsuarioId.ToString(),
                notification.Mensaje,
                Tipo = notification.Tipo.ToString(),
                Estado = notification.Estado.ToString(),
                FechaEnvio = notification.FechaEnvio.ToString("O", CultureInfo.InvariantCulture)
            },
            cancellationToken: cancellationToken));
    }

    public async Task<IReadOnlyList<Notification>> GetByUserIdAsync(
        Guid usuarioId,
        CancellationToken cancellationToken = default)
    {
        using var connection = connectionFactory.CreateConnection();
        var rows = await connection.QueryAsync<NotificationRow>(new CommandDefinition("""
            SELECT id, usuario_id AS UsuarioId, mensaje, tipo, estado, fecha_envio AS FechaEnvio
            FROM notifications
            WHERE usuario_id = @UsuarioId
            ORDER BY fecha_envio
            """,
            new { UsuarioId = usuarioId.ToString() },
            cancellationToken: cancellationToken));

        // Lista ya materializada y ordenada de la más vieja a la más nueva.
        return rows.Select(ToModel).ToList();
    }

    private static Notification ToModel(NotificationRow row) => new()
    {
        Id = Guid.Parse(row.Id),
        UsuarioId = Guid.Parse(row.UsuarioId),
        Mensaje = row.Mensaje,
        Tipo = Enum.Parse<NotificationType>(row.Tipo),
        Estado = Enum.Parse<NotificationStatus>(row.Estado),
        FechaEnvio = DateTime.Parse(row.FechaEnvio, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind)
    };

    // Una fila tal como la devuelve SQLite.
    private sealed class NotificationRow
    {
        public string Id { get; set; } = string.Empty;
        public string UsuarioId { get; set; } = string.Empty;
        public string Mensaje { get; set; } = string.Empty;
        public string Tipo { get; set; } = string.Empty;
        public string Estado { get; set; } = string.Empty;
        public string FechaEnvio { get; set; } = string.Empty;
    }
}
