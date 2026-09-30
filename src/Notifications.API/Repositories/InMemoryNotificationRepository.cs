using Notifications.API.Models;

namespace Notifications.API.Repositories;

public class InMemoryNotificationRepository : INotificationRepository
{
    private readonly List<Notification> _notifications = new();

    public Task AgregarAsync(Notification notification, CancellationToken cancellationToken = default)
    {
        _notifications.Add(notification);
        return Task.CompletedTask;
    }

    public Task<IEnumerable<Notification>> ObtenerPorUsuarioAsync(Guid usuarioId, CancellationToken cancellationToken = default)
    {
        var result = _notifications.Where(n => n.UsuarioId == usuarioId);
        return Task.FromResult(result);
    }
}