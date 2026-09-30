using Microsoft.AspNetCore.Diagnostics;
using Products.API.Exceptions;

namespace Products.API.ExceptionHandlers;

public class NotFoundExceptionHandler(ErrorResponseWriter writer) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken cancellationToken)
    {
        if (exception is not NotFoundException ex)
        {
            return false;
        }

        await writer.WriteAsync(context, StatusCodes.Status404NotFound, ex.ErrorCode, ex.Message, cancellationToken: cancellationToken);
        return true;
    }
}
