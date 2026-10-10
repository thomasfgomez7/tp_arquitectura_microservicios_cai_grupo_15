using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Orders.API.Infrastructure;

/// <summary>
/// Respuesta JSON de los health checks (sección 5.4 del enunciado): estado general
/// (Healthy, Degraded o Unhealthy) y el detalle de cada check.
/// </summary>
public static class HealthCheckResponseWriter
{
    public static Task WriteAsync(HttpContext context, HealthReport report)
    {
        var servicio = context.RequestServices.GetRequiredService<IHostEnvironment>().ApplicationName;

        return context.Response.WriteAsJsonAsync(new
        {
            status = report.Status.ToString(),
            servicio,
            totalDurationMs = Math.Round(report.TotalDuration.TotalMilliseconds, 2),
            checks = report.Entries.Select(entry => new
            {
                name = entry.Key,
                status = entry.Value.Status.ToString(),
                description = entry.Value.Description,
                durationMs = Math.Round(entry.Value.Duration.TotalMilliseconds, 2)
            })
        });
    }
}
