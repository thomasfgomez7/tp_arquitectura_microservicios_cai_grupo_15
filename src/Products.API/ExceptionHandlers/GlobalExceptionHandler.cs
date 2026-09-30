using Microsoft.AspNetCore.Diagnostics;
using Microsoft.Extensions.Options;
using Products.API.Exceptions;

namespace Products.API.ExceptionHandlers;

/// <summary>
/// Red de seguridad: atrapa cualquier excepción que los handlers específicos no manejaron (PRD-005).
/// Se registra último.
/// </summary>
public class GlobalExceptionHandler(ErrorResponseWriter writer, IOptions<ErrorHandlingOptions> options) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken cancellationToken)
    {
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
