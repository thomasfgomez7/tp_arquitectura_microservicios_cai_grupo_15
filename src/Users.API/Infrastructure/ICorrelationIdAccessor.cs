namespace Users.API.Infrastructure;

public interface ICorrelationIdAccessor
{
    string? CorrelationId { get; }
}