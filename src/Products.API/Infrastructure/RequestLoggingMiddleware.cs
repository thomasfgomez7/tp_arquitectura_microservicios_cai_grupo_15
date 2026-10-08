using System.Diagnostics;
using Serilog.Context;

namespace Products.API.Infrastructure;

/// <summary>
/// Loguea el inicio y el fin de cada request con su duración (sección 5.3 del enunciado).
/// Va después de CorrelationIdMiddleware y antes de UseExceptionHandler, para registrar el status final
/// y el errorCode que dejó el handler de errores.
/// </summary>
public class RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var endpoint = $"{context.Request.Method} {context.Request.Path}";

        using (LogContext.PushProperty("Endpoint", endpoint))
        {
            logger.LogInformation("Inicio del request");
            var inicio = Stopwatch.GetTimestamp();

            try
            {
                await next(context);
            }
            finally
            {
                var elapsedMs = Math.Round(Stopwatch.GetElapsedTime(inicio).TotalMilliseconds, 2);
                var errorCode = context.Items[HttpContextItemKeys.ErrorCode] as string;

                using (LogContext.PushProperty("ErrorCode", errorCode))
                {
                    logger.LogInformation(
                        "Fin del request: respondió {StatusCode} en {ElapsedMs} ms",
                        context.Response.StatusCode, elapsedMs);
                }
            }
        }
    }
}
