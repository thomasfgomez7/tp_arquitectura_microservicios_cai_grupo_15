using Notifications.API.Models;

namespace Notifications.API.Repositories;

public interface INotificationRepository
{
    Task AgregarAsync(Notification notification, CancellationToken cancellationToken = default);
    Task<IEnumerable<Notification>> ObtenerPorUsuarioAsync(Guid usuarioId, CancellationToken cancellationToken = default);
}