namespace Notifications.API.Infrastructure;

public class CorrelationIdAccessor(IHttpContextAccessor httpContextAccessor)
    : ICorrelationIdAccessor
{
    public string? CorrelationId =>
        httpContextAccessor.HttpContext?.Items[HttpContextItemKeys.CorrelationId] as string;
}