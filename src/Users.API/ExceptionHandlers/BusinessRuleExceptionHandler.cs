using Microsoft.AspNetCore.Diagnostics;
using Users.API.Exceptions;

namespace Users.API.ExceptionHandlers;

public class BusinessRuleExceptionHandler(
    ErrorResponseWriter writer,
    ILogger<BusinessRuleExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext context,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is not BusinessRuleException ex)
        {
            return false;
        }

        logger.LogWarning(
            "Regla de negocio violada {ErrorCode}: {ErrorMessage}",
            ex.ErrorCode,
            ex.Message);

        await writer.WriteAsync(
            context,
            ex.StatusCode,
            ex.ErrorCode,
            ex.Message,
            cancellationToken: cancellationToken);

        return true;
    }
}