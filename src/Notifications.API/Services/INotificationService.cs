using Notifications.API.DTOs;

namespace Notifications.API.Services;

public interface INotificationService
{
    Task<NotificationResponse> SendAsync(
        SendNotificationRequest request,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<NotificationResponse>> GetByUserIdAsync(
        Guid usuarioId,
        CancellationToken cancellationToken = default);
}