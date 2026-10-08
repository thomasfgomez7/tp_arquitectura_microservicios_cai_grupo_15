using Microsoft.AspNetCore.Diagnostics;
using Notifications.API.Exceptions;

namespace Notifications.API.ExceptionHandlers;

public class ValidationExceptionHandler(
    ErrorResponseWriter writer,
    ILogger<ValidationExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext context,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is not ValidationException ex)
        {
            return false;
        }

        logger.LogWarning(
            "Datos inválidos {ErrorCode}: {ErrorMessage}",
            ex.ErrorCode,
            ex.Message);

        await writer.WriteAsync(
            context,
            StatusCodes.Status400BadRequest,
            ex.ErrorCode,
            ex.Message,
            cancellationToken: cancellationToken);

        return true;
    }
}