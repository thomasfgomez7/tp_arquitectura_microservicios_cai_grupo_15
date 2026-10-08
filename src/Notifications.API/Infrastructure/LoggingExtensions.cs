using Serilog;
using Serilog.Formatting.Json;

namespace Notifications.API.Infrastructure;

/// <summary>
/// Configuración de Serilog (sección 5.3 del enunciado). Se llama antes que cualquier otra configuración.
/// </summary>
public static class LoggingExtensions
{
    private const string ConsoleTemplate =
        "[{Timestamp:HH:mm:ss} {Level:u3}] {Servicio} {CorrelationId} {Endpoint} {Message:lj}{NewLine}{Exception}";

    public static IServiceCollection AddSerilogLogging(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        // Se registra como servicio y con preserveStaticLogger: true para que cada host use su propio
        // logger y no el Log.Logger estático. Si no, cuando los tests levantan varias APIs en paralelo,
        // la última en arrancar reemplaza el logger de las demás y sus logs se pierden.
        services.AddSerilog(preserveStaticLogger: true, configureLogger: (serviceProvider, logger) =>
        {
            logger
                .ReadFrom.Configuration(configuration)      // niveles mínimos (sección "Serilog")
                .ReadFrom.Services(serviceProvider)         // sinks registrados en DI (los usan los tests)
                .Enrich.FromLogContext()                    // CorrelationId, Endpoint y ErrorCode
                .Enrich.WithProperty("Servicio", environment.ApplicationName)
                .WriteTo.Console(outputTemplate: ConsoleTemplate);

            if (configuration.GetValue("LogFile:Enabled", defaultValue: true))
            {
                var path = Path.Combine(environment.ContentRootPath, configuration["LogFile:Path"] ?? "logs/log-.json");
                logger.WriteTo.File(
                    new JsonFormatter(renderMessage: true),
                    path,
                    rollingInterval: RollingInterval.Day);
            }
        });

        return services;
    }
}
