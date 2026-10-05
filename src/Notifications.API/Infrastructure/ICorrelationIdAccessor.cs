namespace Notifications.API.Infrastructure;

public interface ICorrelationIdAccessor
{
    string? CorrelationId { get; }
}