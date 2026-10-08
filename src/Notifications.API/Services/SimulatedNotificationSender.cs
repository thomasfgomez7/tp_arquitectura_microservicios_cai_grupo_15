using Notifications.API.Models;

namespace Notifications.API.Services;

/// <summary>
/// Simula el envío: siempre lo da por enviado, con la fecha actual.
/// </summary>
public class SimulatedNotificationSender(TimeProvider timeProvider) : INotificationSender
{
    public Task<NotificationSendResult> SendAsync(
        Notification notification,
        CancellationToken cancellationToken = default)
    {
        var result = new NotificationSendResult(
            Estado: NotificationStatus.Enviada,
            FechaEnvio: timeProvider.GetUtcNow().UtcDateTime);

        return Task.FromResult(result);
    }
}