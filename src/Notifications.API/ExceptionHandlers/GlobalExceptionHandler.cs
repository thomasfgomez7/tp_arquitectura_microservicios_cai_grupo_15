using Microsoft.AspNetCore.Diagnostics;
using Microsoft.Extensions.Options;
using Notifications.API.Exceptions;

namespace Notifications.API.ExceptionHandlers;

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
            ErrorCodes.NTF_004,
            exception.Message);

        var detail = options.Value.IncludeExceptionDetails
            ? exception.Message
            : null;

        await writer.WriteAsync(
            context,
            StatusCodes.Status500InternalServerError,
            ErrorCodes.NTF_004,
            "Error interno al procesar la notificación.",
            detail,
            cancellationToken);

        return true;
    }
}