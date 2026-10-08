using Notifications.API.Models;

namespace Notifications.API.Services;

/// <summary>
/// Envía la notificación. Hoy es simulado; se puede cambiar por un envío real sin tocar el servicio.
/// </summary>
public interface INotificationSender
{
    Task<NotificationSendResult> SendAsync(
        Notification notification,
        CancellationToken cancellationToken = default);
}