namespace Notifications.API.DTOs;

/// <summary>
/// Notificación tal como la devuelve la API.
/// </summary>
public record NotificationResponse
{
    /// <summary>ID de la notificación.</summary>
    /// <example>11112222-3333-4444-5555-666677778888</example>
    public Guid Id { get; init; }

    /// <summary>ID del usuario destinatario.</summary>
    /// <example>a1b2c3d4-0000-0000-0000-111122223333</example>
    public Guid UsuarioId { get; init; }

    /// <example>Su orden #f1e2d3c4 fue confirmada.</example>
    public string Mensaje { get; init; } = string.Empty;

    /// <summary>Email, Push o SMS.</summary>
    /// <example>Email</example>
    public string Tipo { get; init; } = string.Empty;

    /// <summary>Pendiente, Enviada o Fallida.</summary>
    /// <example>Enviada</example>
    public string Estado { get; init; } = string.Empty;

    /// <summary>Fecha de envío (UTC).</summary>
    /// <example>2024-03-10T12:01:00Z</example>
    public DateTime FechaEnvio { get; init; }
}
