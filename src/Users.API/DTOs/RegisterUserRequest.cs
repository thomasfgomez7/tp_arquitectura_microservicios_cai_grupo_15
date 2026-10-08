using System.ComponentModel.DataAnnotations;

namespace Users.API.DTOs;

/// <summary>
/// Datos para registrar un usuario (POST /api/users/register).
/// </summary>
public record RegisterUserRequest
{
    /// <example>María</example>
    [Required(ErrorMessage = "El nombre es obligatorio.")]
    public string Nombre { get; init; } = string.Empty;

    /// <example>González</example>
    [Required(ErrorMessage = "El apellido es obligatorio.")]
    public string Apellido { get; init; } = string.Empty;

    /// <example>maria@email.com</example>
    [Required(ErrorMessage = "El email es obligatorio.")]
    [RegularExpression(EmailFormat.Pattern, ErrorMessage = "El email no tiene un formato válido.")]
    public string Email { get; init; } = string.Empty;

    /// <example>MiPassword123!</example>
    [Required(ErrorMessage = "La contraseña es obligatoria.")]
    public string Password { get; init; } = string.Empty;
}