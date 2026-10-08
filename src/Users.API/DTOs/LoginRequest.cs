using System.ComponentModel.DataAnnotations;

namespace Users.API.DTOs;

/// <summary>
/// Datos para iniciar sesión (POST /api/users/login).
/// </summary>
public record LoginRequest
{
    /// <example>maria@email.com</example>
    [Required(ErrorMessage = "El email es obligatorio.")]
    [RegularExpression(EmailFormat.Pattern, ErrorMessage = "El email no tiene un formato válido.")]
    public string Email { get; init; } = string.Empty;

    /// <example>MiPassword123!</example>
    [Required(ErrorMessage = "La contraseña es obligatoria.")]
    public string Password { get; init; } = string.Empty;
}