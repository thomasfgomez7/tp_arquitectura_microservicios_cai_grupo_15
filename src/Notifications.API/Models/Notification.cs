namespace Notifications.API.Models;

/// <summary>
/// Notificación registrada (Apéndice A del enunciado).
/// Id lo asigna NotificationService; Estado y FechaEnvio, el INotificationSender.
/// </summary>
public class Notification
{
    public Guid Id { get; set; }
    public Guid UsuarioId { get; set; }
    public string Mensaje { get; set; } = string.Empty;
    public NotificationType Tipo { get; set; }
    public NotificationStatus Estado { get; set; }
    public DateTime FechaEnvio { get; set; }
}