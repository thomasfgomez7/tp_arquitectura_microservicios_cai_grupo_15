namespace Users.API.DTOs;

public record UserResponse
{
    public Guid Id { get; init; }
    public string Nombre { get; init; } = string.Empty;
    public string Apellido { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public DateTime FechaRegistro { get; init; }
    public bool Activo { get; init; }
}