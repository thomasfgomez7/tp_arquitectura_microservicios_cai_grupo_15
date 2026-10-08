using Microsoft.Extensions.Diagnostics.HealthChecks;
using Notifications.API.Repositories;

namespace Notifications.API.Infrastructure;

/// <summary>
/// Verifica que la persistencia responda. Hoy es el repositorio en memoria; cuando llegue la librería
/// de la cátedra, este check pasa a verificar la conexión real sin cambiar nada más.
/// </summary>
public class NotificationRepositoryHealthCheck(INotificationRepository repository) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            await repository.GetByUserIdAsync(Guid.Empty, cancellationToken);
            return HealthCheckResult.Healthy("La persistencia de notificaciones responde.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("La persistencia de notificaciones no responde.", ex);
        }
    }
}
