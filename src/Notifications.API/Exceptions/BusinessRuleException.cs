namespace Notifications.API.Exceptions;

public class BusinessRuleException(
    string errorCode,
    string message,
    int statusCode) : Exception(message)
{
    public string ErrorCode { get; } = errorCode;

    public int StatusCode { get; } = statusCode;
}