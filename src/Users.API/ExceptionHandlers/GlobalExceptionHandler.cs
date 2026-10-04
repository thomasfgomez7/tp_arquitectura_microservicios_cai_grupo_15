using Microsoft.AspNetCore.Diagnostics;
using Microsoft.Extensions.Options;
using Users.API.Exceptions;

namespace Users.API.ExceptionHandlers;

public class GlobalExceptionHandler(
    ErrorResponseWriter writer,
    IOptions<ErrorHandlingOptions> options,
    ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext context,
        Exception exception,
        CancellationToken cancellationToken)
    {
        logger.LogError(
            exception,
            "Error inesperado {ErrorCode}: {ErrorMessage}",
            ErrorCodes.USR_006,
            exception.Message);

        var detail = options.Value.IncludeExceptionDetails
            ? exception.Message
            : null;

        await writer.WriteAsync(
            context,
            StatusCodes.Status500InternalServerError,
            ErrorCodes.USR_006,
            "Error interno al procesar el usuario.",
            detail,
            cancellationToken);

        return true;
    }
}