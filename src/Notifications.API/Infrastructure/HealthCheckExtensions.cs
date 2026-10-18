using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Notifications.API.Clients;

namespace Notifications.API.Infrastructure;

/// <summary>
/// Health checks (sección 5.4 del enunciado):
/// /health/live → el proceso está vivo (no depende de nada externo);
/// /health/ready → puede atender requests (persistencia y Users.API);
/// /health → todos los checks.
/// </summary>
public static class HealthCheckExtensions
{
    private const string LiveTag = "live";
    private const string ReadyTag = "ready";

    public static IServiceCollection AddNotificationHealthChecks(this IServiceCollection services)
    {
        services.AddHealthChecks()
            .AddCheck("self", () => HealthCheckResult.Healthy("El servicio está en ejecución."), tags: [LiveTag])
            .AddCheck<NotificationRepositoryHealthCheck>("persistencia", tags: [ReadyTag])
            // Users caído degrada el servicio pero no lo saca de servicio: se pueden listar las
            // notificaciones; fallan los envíos (D-24, D-29).
            .Add(new HealthCheckRegistration(
                "Users.API",
                provider => new DownstreamServiceHealthCheck(
                    provider.GetRequiredService<IHttpClientFactory>(), nameof(IUsersClient), "Users.API"),
                HealthStatus.Degraded,
                [ReadyTag]));

        return services;
    }

    public static WebApplication MapNotificationHealthChecks(this WebApplication app)
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
