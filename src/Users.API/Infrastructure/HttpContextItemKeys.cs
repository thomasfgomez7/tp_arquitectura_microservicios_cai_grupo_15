namespace Users.API.Infrastructure;

/// <summary>
/// Claves de HttpContext.Items que comparten los middlewares y los exception handlers durante un request.
/// </summary>
public static class HttpContextItemKeys
{
    public const string CorrelationId = "CorrelationId";

    public const string ErrorCode = "ErrorCode";
}
