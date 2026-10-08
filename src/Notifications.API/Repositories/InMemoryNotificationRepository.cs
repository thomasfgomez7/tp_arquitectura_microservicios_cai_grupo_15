using System.Collections.Concurrent;
using Notifications.API.Models;

namespace Notifications.API.Repositories;

/// <summary>
/// Persistencia en memoria hasta recibir la librería de la cátedra (D-03).
/// Se registra como Singleton para que los datos sobrevivan entre requests.
/// </summary>
public class InMemoryNotificationRepository : INotificationRepository
{
    private readonly ConcurrentDictionary<Guid, Notification> _notifications = new();

    public Task AddAsync(Notification notification, CancellationToken cancellationToken = default)
    {
        _notifications[notification.Id] = notification;
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<Notification>> GetByUserIdAsync(
        Guid usuarioId,
        CancellationToken cancellationToken = default)
    {
        // Lista ya materializada y ordenada de la más vieja a la más nueva.
        IReadOnlyList<Notification> result = _notifications.Values
            .Where(notification => notification.UsuarioId == usuarioId)
            .OrderBy(notification => notification.FechaEnvio)
            .ToList();

        return Task.FromResult(result);
    }
}