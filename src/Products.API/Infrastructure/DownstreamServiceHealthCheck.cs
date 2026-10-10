using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Products.API.Infrastructure;

/// <summary>
/// Verifica que responda un servicio del que se depende, llamando a su /health/live con el mismo
/// HttpClient que usa el cliente tipado (misma URL base y timeout).
/// Se consulta /health/live y no /health/ready para no encadenar los chequeos de todo el sistema.
/// </summary>
public class DownstreamServiceHealthCheck(
    IHttpClientFactory httpClientFactory,
    string clientName,
    string serviceName) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            using var response = await httpClientFactory.CreateClient(clientName).GetAsync("health/live", cancellationToken);
            response.EnsureSuccessStatusCode();

            return HealthCheckResult.Healthy($"{serviceName} responde.");
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            // El estado de falla lo define el registro: Degraded (D-24).
            return new HealthCheckResult(context.Registration.FailureStatus, $"{serviceName} no responde.", ex);
        }
    }
}
