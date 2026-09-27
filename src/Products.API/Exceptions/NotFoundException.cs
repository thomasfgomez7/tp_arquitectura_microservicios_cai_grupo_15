namespace Products.API.Exceptions;

/// <summary>
/// El recurso solicitado no existe. Se traduce a una respuesta 404.
/// </summary>
public class NotFoundException(string errorCode, string message) : Exception(message)
{
    public string ErrorCode { get; } = errorCode;
}
