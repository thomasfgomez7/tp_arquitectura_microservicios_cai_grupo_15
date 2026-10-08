namespace Notifications.API.Models;

/// <summary>
/// Canal por el que se envía la notificación (Apéndice A del enunciado).
/// </summary>
public enum NotificationType
{
    Email,
    Push,
    SMS
}