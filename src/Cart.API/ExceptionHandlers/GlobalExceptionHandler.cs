using Microsoft.AspNetCore.Diagnostics;
using Microsoft.Extensions.Options;
using Cart.API.Exceptions;

namespace Cart.API.ExceptionHandlers;

/// <summary>
/// Red de seguridad: atrapa cualquier excepción que los handlers específicos no manejaron (CRT-005).
/// Se registra último. El stack trace va al log, nunca a la respuesta.
/// </summary>
public class GlobalExceptionHandler(
    ErrorResponseWriter writer,
    IOptions<ErrorHandlingOptions> options,
    ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken cancellationToken)
    {
        logger.LogError(exception, "Error inesperado {ErrorCode}: {ErrorMessage}", ErrorCodes.CRT_005, exception.Message);

        var detail = options.Value.IncludeExceptionDetails ? exception.Message : null;

        await writer.WriteAsync(
            context,
            StatusCodes.Status500InternalServerError,
            ErrorCodes.CRT_005,
            "Error interno al procesar el carrito.",
            detail,
            cancellationToken);

        return true;
    }
}
