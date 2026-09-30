using Microsoft.AspNetCore.Diagnostics;
using Products.API.Exceptions;

namespace Products.API.ExceptionHandlers;

public class BusinessRuleExceptionHandler(ErrorResponseWriter writer) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken cancellationToken)
    {
        if (exception is not BusinessRuleException ex)
        {
            return false;
        }

        await writer.WriteAsync(context, ex.StatusCode, ex.ErrorCode, ex.Message, cancellationToken: cancellationToken);
        return true;
    }
}
