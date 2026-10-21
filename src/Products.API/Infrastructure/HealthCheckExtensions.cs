using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Products.API.Clients;

namespace Products.API.Infrastructure;

/// <summary>
/// Health checks (sección 5.4 del enunciado):
/// /health/live → el proceso está vivo (no depende de nada externo);
/// /health/ready → puede atender requests (persistencia y Orders.API, del que depende el DELETE);
/// /health → todos los checks.
/// </summary>
public static class HealthCheckExtensions
{
    private const string LiveTag = "live";
    private const string ReadyTag = "ready";

    public static IServiceCollection AddProductHealthChecks(this IServiceCollection services)
    {
        services.AddHealthChecks()
            .AddCheck("self", () => HealthCheckResult.Healthy("El servicio está en ejecución."), tags: [LiveTag])
            .AddCheck<ProductRepositoryHealthCheck>("persistencia", tags: [ReadyTag])
            // Orders caído degrada el servicio pero no lo saca de servicio: solo falla el DELETE (D-24).
            .Add(new HealthCheckRegistration(
                "Orders.API",
                provider => new DownstreamServiceHealthCheck(
                    provider.GetRequiredService<IHttpClientFactory>(), nameof(IOrdersClient), "Orders.API"),
                HealthStatus.Degraded,
                [ReadyTag]));

        return services;
    }

    public static WebApplication MapProductHealthChecks(this WebApplication app)
    {
        app.MapHealthChecks("/health", Options(_ => true));
        app.MapHealthChecks("/health/ready", Options(check => check.Tags.Contains(ReadyTag)));
        app.MapHealthChecks("/health/live", Options(check => check.Tags.Contains(LiveTag)));

        return app;
    }

    private static HealthCheckOptions Options(Func<HealthCheckRegistration, bool> predicate) => new()
    {
        Predicate = predicate,
        ResponseWriter = HealthCheckResponseWriter.WriteAsync
    };
}
