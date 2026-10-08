using Microsoft.Extensions.Diagnostics.HealthChecks;
using Products.API.Repositories;

namespace Products.API.Infrastructure;

/// <summary>
/// Verifica que la persistencia responda. Hoy es el repositorio en memoria; cuando llegue la librería
/// de la cátedra, este check pasa a verificar la conexión real sin cambiar nada más.
/// </summary>
public class ProductRepositoryHealthCheck(IProductRepository repository) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            await repository.GetAllAsync(cancellationToken);
            return HealthCheckResult.Healthy("La persistencia de productos responde.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("La persistencia de productos no responde.", ex);
        }
    }
}
