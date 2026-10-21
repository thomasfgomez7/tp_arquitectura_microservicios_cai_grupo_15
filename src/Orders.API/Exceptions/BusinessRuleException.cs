namespace Orders.API.Exceptions;

/// <summary>
/// Se violó una regla de negocio. Lleva el status HTTP porque el catálogo usa 401, 403, 409 y 422 (D-10).
/// </summary>
public class BusinessRuleException(string errorCode, string message, int statusCode) : Exception(message)
{
    public string ErrorCode { get; } = errorCode;

    public int StatusCode { get; } = statusCode;
}
