namespace Users.API.DTOs;

/// <summary>
/// Datos del usuario autenticado (POST /api/users/login).
/// </summary>
public record LoginResponse
{
    /// <summary>ID del usuario.</summary>
    /// <example>a1b2c3d4-0000-0000-0000-111122223333</example>
    public Guid Id { get; init; }

    /// <example>María</example>
    public string Nombre { get; init; } = string.Empty;

    /// <example>González</example>
    public string Apellido { get; init; } = string.Empty;

    /// <example>maria@email.com</example>
    public string Email { get; init; } = string.Empty;
}
