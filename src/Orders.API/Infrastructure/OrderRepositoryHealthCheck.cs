using Microsoft.Extensions.Diagnostics.HealthChecks;
using Orders.API.Repositories;

namespace Orders.API.Infrastructure;

/// <summary>
/// Verifica que la persistencia responda. Hoy es el repositorio en memoria; cuando llegue la librería
/// de la cátedra, este check pasa a verificar la conexión real sin cambiar nada más.
/// </summary>
public class OrderRepositoryHealthCheck(IOrderRepository repository) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            await repository.GetByIdAsync(Guid.Empty, cancellationToken);
            return HealthCheckResult.Healthy("La persistencia de órdenes responde.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("La persistencia de órdenes no responde.", ex);
        }
    }
}
