namespace Notifications.API.Exceptions;

public class BusinessRuleException(string errorCode, string message) : Exception(message)
{
    public string ErrorCode { get; } = errorCode;
}