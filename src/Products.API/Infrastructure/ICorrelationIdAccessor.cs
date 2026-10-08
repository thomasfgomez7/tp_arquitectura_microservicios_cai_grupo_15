namespace Products.API.Infrastructure;

/// <summary>
/// Expone el Correlation ID del request actual a quien lo necesite (handlers de error y, desde la
/// Etapa 9, las llamadas HTTP salientes) sin que dependan de HttpContext.
/// </summary>
public interface ICorrelationIdAccessor
{
    /// <summary>El Correlation ID del request en curso, o null si no hay un request activo.</summary>
    string? CorrelationId { get; }
}
