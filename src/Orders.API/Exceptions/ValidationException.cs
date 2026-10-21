namespace Orders.API.Exceptions;

/// <summary>
/// Los datos recibidos son inválidos. Se traduce a una respuesta 400.
/// </summary>
/// <remarks>
/// Tiene el mismo nombre que System.ComponentModel.DataAnnotations.ValidationException:
/// si un archivo usa ambos namespaces, hay que calificar el nombre.
/// </remarks>
public class ValidationException(string errorCode, string message) : Exception(message)
{
    public string ErrorCode { get; } = errorCode;
}
