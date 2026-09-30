using Products.API.Exceptions;

namespace Products.API.ExceptionHandlers;

/// <summary>
/// Arma y escribe la respuesta de error del contrato (sección 3.1 del enunciado).
/// Es el único lugar que conoce el formato; los handlers solo deciden status, código y mensaje.
/// </summary>
public class ErrorResponseWriter
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

    // Códigos cuyo "detail" difiere del genérico de su status, según los ejemplos del enunciado.
    private static readonly Dictionary<string, string> DetailByErrorCode = new()
    {
        [ErrorCodes.PRD_004] = "No se puede eliminar el recurso."
    };

    public Task WriteAsync(
        HttpContext context,
        int statusCode,
        string errorCode,
        string errorMessage,
        string? detail = null,
        CancellationToken cancellationToken = default)
    {
        var info = InfoByStatus.GetValueOrDefault(statusCode, InfoByStatus[StatusCodes.Status500InternalServerError]);

        context.Response.StatusCode = statusCode;

        return context.Response.WriteAsJsonAsync(
            new
            {
                type = info.Type,
                title = info.Title,
                status = statusCode,
                detail = detail ?? DetailByErrorCode.GetValueOrDefault(errorCode, info.Detail),
                instance = context.Request.Path.Value,
                errorCode,
                errorMessage
            },
            options: null,
            contentType: "application/problem+json",
            cancellationToken: cancellationToken);
    }
}
