using System.Text.RegularExpressions;
using Serilog.Context;

namespace Orders.API.Infrastructure;

/// <summary>
/// Toma el X-Correlation-Id del request (o genera uno nuevo), lo devuelve en la respuesta y lo agrega
/// a todos los logs del request (sección 5.5 del enunciado). Va primero en el pipeline.
/// </summary>
public partial class CorrelationIdMiddleware(RequestDelegate next)
{
    public const string HeaderName = "X-Correlation-Id";

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = ReadValidHeader(context) ?? Guid.NewGuid().ToString();

        context.Items[HttpContextItemKeys.CorrelationId] = correlationId;

        // OnStarting en lugar de escribir el header ya: si hay una excepción, UseExceptionHandler
        // limpia los headers de la respuesta, y así el header se agrega igual al enviarla.
        context.Response.OnStarting(() =>
        {
            context.Response.Headers[HeaderName] = correlationId;
            return Task.CompletedTask;
        });

        using (LogContext.PushProperty("CorrelationId", correlationId))
        {
            await next(context);
        }
    }

    // Solo se acepta un valor corto y con caracteres seguros: el valor viene del cliente
    // y termina escrito en los logs y en otros servicios.
    private static string? ReadValidHeader(HttpContext context)
    {
        var value = context.Request.Headers[HeaderName].ToString();
        return ValidCorrelationId().IsMatch(value) ? value : null;
    }

    [GeneratedRegex("^[A-Za-z0-9._-]{1,64}$")]
    private static partial Regex ValidCorrelationId();
}
