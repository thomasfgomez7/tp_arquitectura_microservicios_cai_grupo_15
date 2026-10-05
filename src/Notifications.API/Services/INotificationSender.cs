using Notifications.API.Models;

namespace Notifications.API.Services;

public interface INotificationSender
{
    Task<NotificationSendResult> SendAsync(
        Notification notification,
        CancellationToken cancellationToken = default);
}

public record NotificationSendResult(string Estado, DateTime FechaEnvio);