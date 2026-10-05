using System.Collections.Concurrent;
using Notifications.API.Models;

namespace Notifications.API.Repositories;

public class InMemoryNotificationRepository : INotificationRepository
{
    private readonly ConcurrentDictionary<Guid, Notification> _notifications = new();

    public Task AgregarAsync(
        Notification notification,
        CancellationToken cancellationToken = default)
    {
        _notifications[notification.Id] = notification;
        return Task.CompletedTask;
    }

    public Task<IEnumerable<Notification>> ObtenerPorUsuarioAsync(
        Guid usuarioId,
        CancellationToken cancellationToken = default)
    {
        IEnumerable<Notification> result = _notifications.Values
            .Where(notification => notification.UsuarioId == usuarioId)
            .ToArray();

        return Task.FromResult(result);
    }
}