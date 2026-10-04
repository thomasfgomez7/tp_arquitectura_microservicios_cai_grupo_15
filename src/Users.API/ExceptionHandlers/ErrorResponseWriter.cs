using Users.API.DTOs;
using Users.API.Infrastructure;

namespace Users.API.ExceptionHandlers;

public class ErrorResponseWriter(ICorrelationIdAccessor correlationIdAccessor)
{
    private sealed record StatusInfo(string Type, string Title, string Detail);

    private static readonly Dictionary<int, StatusInfo> InfoByStatus = new()
    {
        [StatusCodes.Status400BadRequest] = new(
            "https://tools.ietf.org/html/rfc7231#section-6.5.1",
            "Bad Request",
            "La solicitud contiene datos inválidos."),

        [StatusCodes.Status401Unauthorized] = new(
            "https://tools.ietf.org/html/rfc7235#section-3.1",
            "Unauthorized",
            "Las credenciales no son válidas."),

        [StatusCodes.Status403Forbidden] = new(
            "https://tools.ietf.org/html/rfc7231#section-6.5.3",
            "Forbidden",
            "El acceso está prohibido."),

        [StatusCodes.Status404NotFound] = new(
            "https://tools.ietf.org/html/rfc7231#section-6.5.4",
            "Not Found",
            "El recurso solicitado no fue encontrado."),

        [StatusCodes.Status409Conflict] = new(
            "https://tools.ietf.org/html/rfc7231#section-6.5.9",
            "Conflict",
            "Ya existe un recurso con esos datos."),

        [StatusCodes.Status500InternalServerError] = new(
            "https://tools.ietf.org/html/rfc7231#section-6.6.1",
            "Internal Server Error",
            "Ocurrió un error inesperado al procesar la solicitud.")
    };

    public const string ContentType = "application/problem+json";

    public async Task WriteAsync(
        HttpContext context,
        int statusCode,
        string errorCode,
        string errorMessage,
        string? detail = null,
        CancellationToken cancellationToken = default)
    {
        var statusInfo = InfoByStatus.GetValueOrDefault(
            statusCode,
            InfoByStatus[StatusCodes.Status500InternalServerError]);

        var response = new ErrorResponse
        {
            Type = statusInfo.Type,
            Title = statusInfo.Title,
            Status = statusCode,
            Detail = detail ?? statusInfo.Detail,
            Instance = context.Request.Path.Value,
            ErrorCode = errorCode,
            ErrorMessage = errorMessage,
            CorrelationId = correlationIdAccessor.CorrelationId
        };

        context.Response.StatusCode = statusCode;

        await context.Response.WriteAsJsonAsync(
            response,
            options: null,
            contentType: ContentType,
            cancellationToken: cancellationToken);
    }
}