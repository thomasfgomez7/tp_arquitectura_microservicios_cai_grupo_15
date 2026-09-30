using Notifications.API.DTOs;

namespace Notifications.API.Services;

public interface INotificationService
{
    Task<NotificationResponse> SendAsync(SendNotificationRequest request, CancellationToken cancellationToken = default);
    Task<IEnumerable<NotificationResponse>> GetByUserIdAsync(Guid usuarioId, CancellationToken cancellationToken = default);
}