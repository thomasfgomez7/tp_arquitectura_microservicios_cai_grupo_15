namespace Users.API.DTOs;

public record ErrorResponse
{
    public required string Type { get; init; }
    public required string Title { get; init; }
    public required int Status { get; init; }
    public required string Detail { get; init; }
    public string? Instance { get; init; }
    public required string ErrorCode { get; init; }
    public required string ErrorMessage { get; init; }
    public string? CorrelationId { get; init; }
}