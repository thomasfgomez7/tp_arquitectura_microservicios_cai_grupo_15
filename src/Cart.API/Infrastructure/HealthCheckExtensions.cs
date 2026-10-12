using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Cart.API.Clients;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Cart.API.Infrastructure;

/// <summary>
/// Health checks (sección 5.4 del enunciado):
/// /health/live → el proceso está vivo (no depende de nada externo);
/// /health/ready → puede atender requests (persistencia y Products.API);
/// /health → todos los checks.
/// </summary>
public static class HealthCheckExtensions
{
    private const string LiveTag = "live";
    private const string ReadyTag = "ready";

    public static IServiceCollection AddCartHealthChecks(this IServiceCollection services)
    {
        services.AddHealthChecks()
            .AddCheck("self", () => HealthCheckResult.Healthy("El servicio está en ejecución."), tags: [LiveTag])
            .AddCheck<CartRepositoryHealthCheck>("persistencia", tags: [ReadyTag])
            // Products caído degrada el servicio pero no lo saca de servicio: el carrito se puede
            // consultar y vaciar; fallan las altas y los cambios de cantidad (D-24).
            .Add(new HealthCheckRegistration(
                "Products.API",
                provider => new DownstreamServiceHealthCheck(
                    provider.GetRequiredService<IHttpClientFactory>(), nameof(IProductsClient), "Products.API"),
                HealthStatus.Degraded,
                [ReadyTag]));

        return services;
    }

    public static WebApplication MapCartHealthChecks(this WebApplication app)
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
