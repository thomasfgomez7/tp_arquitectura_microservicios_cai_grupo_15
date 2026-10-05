using Notifications.API.Models;

namespace Notifications.API.Services;

public class SimulatedNotificationSender(TimeProvider timeProvider)
    : INotificationSender
{
    public Task<NotificationSendResult> SendAsync(
        Notification notification,
        CancellationToken cancellationToken = default)
    {
        var result = new NotificationSendResult(
            Estado: "Enviada",
            FechaEnvio: timeProvider.GetUtcNow().UtcDateTime);

        return Task.FromResult(result);
    }
}