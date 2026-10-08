using Notifications.API.Models;

namespace Notifications.API.Repositories;

/// <summary>
/// Persistencia de notificaciones. Cuando llegue la librería de la cátedra solo cambia la implementación (D-03).
/// </summary>
public interface INotificationRepository
{
    Task AddAsync(Notification notification, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Notification>> GetByUserIdAsync(Guid usuarioId, CancellationToken cancellationToken = default);
}