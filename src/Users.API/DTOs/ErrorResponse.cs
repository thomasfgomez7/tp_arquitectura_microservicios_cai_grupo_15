namespace Users.API.DTOs;

/// <summary>
/// Respuesta de error de todas las respuestas 4xx y 5xx (sección 3.1 del enunciado).
/// </summary>
public record ErrorResponse
{
    /// <summary>URI de la RFC que describe el status HTTP.</summary>
    /// <example>https://tools.ietf.org/html/rfc7231#section-6.5.9</example>
    public required string Type { get; init; }

    /// <summary>Nombre del status HTTP.</summary>
    /// <example>Conflict</example>
    public required string Title { get; init; }

    /// <summary>Código de status HTTP.</summary>
    /// <example>409</example>
    public required int Status { get; init; }

    /// <summary>Descripción general del tipo de error.</summary>
    /// <example>Ya existe un recurso con esos datos.</example>
    public required string Detail { get; init; }

    /// <summary>Ruta del request que produjo el error.</summary>
    /// <example>/api/users/register</example>
    public string? Instance { get; init; }

    /// <summary>Código del catálogo de errores (USR-001 a USR-007).</summary>
    /// <example>USR-001</example>
    public required string ErrorCode { get; init; }

    /// <summary>Mensaje del catálogo; en errores de validación, todos los problemas separados por "; ".</summary>
    /// <example>El email 'maria@email.com' ya está registrado.</example>
    public required string ErrorMessage { get; init; }

    /// <summary>Correlation ID del request (header X-Correlation-Id), para buscarlo en los logs.</summary>
    /// <example>0f8fad5b-d9cb-469f-a165-70867728950e</example>
    public string? CorrelationId { get; init; }
}
