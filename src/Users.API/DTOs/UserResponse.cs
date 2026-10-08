namespace Users.API.DTOs;

/// <summary>
/// Usuario tal como lo devuelve la API. Nunca incluye PasswordHash ni IntentosFallidos.
/// </summary>
public record UserResponse
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

    /// <summary>Fecha de registro (UTC).</summary>
    /// <example>2024-03-10T09:00:00Z</example>
    public DateTime FechaRegistro { get; init; }

    /// <summary>false cuando el usuario está bloqueado.</summary>
    /// <example>true</example>
    public bool Activo { get; init; }
}
