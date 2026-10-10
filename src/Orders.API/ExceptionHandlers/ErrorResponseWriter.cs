using Orders.API.DTOs;
using Orders.API.Infrastructure;

namespace Orders.API.ExceptionHandlers;

/// <summary>
/// Arma y escribe la respuesta de error del contrato (sección 3.1 del enunciado) más el campo
/// correlationId (D-11). Es el único lugar que conoce el formato: lo usan los exception handlers
/// para las respuestas reales y Swagger para los ejemplos.
/// </summary>
public class ErrorResponseWriter(ICorrelationIdAccessor correlationIdAccessor)
{
    private sealed record StatusInfo(string Type, string Title, string Detail);

    private static readonly Dictionary<int, StatusInfo> InfoByStatus = new()
    {
        [StatusCodes.Status400BadRequest] = new("https://tools.ietf.org/html/rfc7231#section-6.5.1", "Bad Request", "La solicitud contiene datos inválidos."),
        [StatusCodes.Status401Unauthorized] = new("https://tools.ietf.org/html/rfc7235#section-3.1", "Unauthorized", "Las credenciales no son válidas."),
        [StatusCodes.Status403Forbidden] = new("https://tools.ietf.org/html/rfc7231#section-6.5.3", "Forbidden", "El acceso está prohibido."),
        [StatusCodes.Status404NotFound] = new("https://tools.ietf.org/html/rfc7231#section-6.5.4", "Not Found", "El recurso solicitado no fue encontrado."),
        [StatusCodes.Status409Conflict] = new("https://tools.ietf.org/html/rfc7231#section-6.5.9", "Conflict", "Ya existe un recurso con esos datos."),
        [StatusCodes.Status422UnprocessableEntity] = new("https://tools.ietf.org/html/rfc4918#section-11.2", "Unprocessable Entity", "No se puede procesar la solicitud."),
        [StatusCodes.Status500InternalServerError] = new("https://tools.ietf.org/html/rfc7231#section-6.6.1", "Internal Server Error", "Ocurrió un error inesperado al procesar la solicitud.")
    };

    public const string ContentType = "application/problem+json";

    public static ErrorResponse Build(
        int statusCode,
        string errorCode,
        string errorMessage,
        string? instance,
        string? correlationId,
        string? detail = null)
    {
        var info = InfoByStatus.GetValueOrDefault(statusCode, InfoByStatus[StatusCodes.Status500InternalServerError]);

        return new ErrorResponse
        {
            Type = info.Type,
            Title = info.Title,
            Status = statusCode,
            Detail = detail ?? info.Detail,
            Instance = instance,
            ErrorCode = errorCode,
            ErrorMessage = errorMessage,
            CorrelationId = correlationId
        };
    }

    public Task WriteAsync(
        HttpContext context,
        int statusCode,
        string errorCode,
        string errorMessage,
        string? detail = null,
        CancellationToken cancellationToken = default)
    {
        // Para que el log de fin del request (RequestLoggingMiddleware) incluya el errorCode.
        context.Items[HttpContextItemKeys.ErrorCode] = errorCode;
        context.Response.StatusCode = statusCode;

        var body = Build(statusCode, errorCode, errorMessage, context.Request.Path.Value, correlationIdAccessor.CorrelationId, detail);

        return context.Response.WriteAsJsonAsync(body, options: null, contentType: ContentType, cancellationToken: cancellationToken);
    }
}
