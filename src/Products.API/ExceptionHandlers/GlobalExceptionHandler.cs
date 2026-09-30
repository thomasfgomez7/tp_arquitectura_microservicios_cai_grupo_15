using Microsoft.AspNetCore.Diagnostics;
using Microsoft.Extensions.Options;
using Products.API.Exceptions;

namespace Products.API.ExceptionHandlers;

/// <summary>
/// Red de seguridad: atrapa cualquier excepción que los handlers específicos no manejaron (PRD-005).
/// Se registra último. El stack trace va al log, nunca a la respuesta.
/// </summary>
public class GlobalExceptionHandler(
    ErrorResponseWriter writer,
    IOptions<ErrorHandlingOptions> options,
    ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken cancellationToken)
    {
        logger.LogError(exception, "Error inesperado {ErrorCode}: {ErrorMessage}", ErrorCodes.PRD_005, exception.Message);

        var detail = options.Value.IncludeExceptionDetails ? exception.Message : null;

        await writer.WriteAsync(
            context,
            StatusCodes.Status500InternalServerError,
            ErrorCodes.PRD_005,
            "Error interno al procesar el producto.",
            detail,
            cancellationToken);

        return true;
    }
}
