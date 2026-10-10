namespace Orders.API.ExceptionHandlers;

/// <summary>
/// Nivel de detalle de los errores inesperados, según el entorno (sección "ErrorHandling" de appsettings).
/// </summary>
public class ErrorHandlingOptions
{
    public const string SectionName = "ErrorHandling";

    /// <summary>
    /// Si es true, el "detail" de un 500 incluye el mensaje de la excepción (nunca el stack trace).
    /// Solo se activa en appsettings.Development.json.
    /// </summary>
    public bool IncludeExceptionDetails { get; set; }
}
