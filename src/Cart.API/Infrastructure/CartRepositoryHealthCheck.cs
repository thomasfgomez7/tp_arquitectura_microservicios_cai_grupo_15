using Cart.API.Repositories;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Cart.API.Infrastructure;

/// <summary>
/// Verifica que la persistencia responda. Hoy es el repositorio en memoria; cuando llegue la librería
/// de la cátedra, este check pasa a verificar la conexión real sin cambiar nada más.
/// </summary>
public class CartRepositoryHealthCheck(ICartRepository repository) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            await repository.GetByUserIdAsync(Guid.Empty, cancellationToken);
            return HealthCheckResult.Healthy("La persistencia de carritos responde.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("La persistencia de carritos no responde.", ex);
        }
    }
}
