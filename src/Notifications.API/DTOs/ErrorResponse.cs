namespace Notifications.API.DTOs;

/// <summary>
/// Respuesta de error de todas las respuestas 4xx y 5xx (sección 3.1 del enunciado).
/// </summary>
public record ErrorResponse
{
    /// <summary>URI de la RFC que describe el status HTTP.</summary>
    /// <example>https://tools.ietf.org/html/rfc7231#section-6.5.4</example>
    public required string Type { get; init; }

    /// <summary>Nombre del status HTTP.</summary>
    /// <example>Not Found</example>
    public required string Title { get; init; }

    /// <summary>Código de status HTTP.</summary>
    /// <example>404</example>
    public required int Status { get; init; }

    /// <summary>Descripción general del tipo de error.</summary>
    /// <example>El recurso solicitado no fue encontrado.</example>
    public required string Detail { get; init; }

    /// <summary>Ruta del request que produjo el error.</summary>
    /// <example>/api/notifications/send</example>
    public string? Instance { get; init; }

    /// <summary>Código del catálogo de errores (NTF-001 a NTF-004).</summary>
    /// <example>NTF-001</example>
    public required string ErrorCode { get; init; }

    /// <summary>Mensaje del catálogo; en errores de validación, todos los problemas separados por "; ".</summary>
    /// <example>El usuario destinatario no fue encontrado.</example>
    public required string ErrorMessage { get; init; }

    /// <summary>Correlation ID del request (header X-Correlation-Id), para buscarlo en los logs.</summary>
    /// <example>0f8fad5b-d9cb-469f-a165-70867728950e</example>
    public string? CorrelationId { get; init; }
}
