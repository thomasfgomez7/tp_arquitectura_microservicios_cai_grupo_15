using System.ComponentModel.DataAnnotations;

namespace Notifications.API.DTOs;

/// <summary>
/// Datos para registrar y enviar una notificación (POST /api/notifications/send).
/// </summary>
public record SendNotificationRequest
{
    /// <summary>ID del usuario destinatario. Obligatorio. Es Guid? para que [Required] detecte si falta.</summary>
    /// <example>a1b2c3d4-0000-0000-0000-111122223333</example>
    [Required(ErrorMessage = "El usuario es obligatorio.")]
    public Guid? UsuarioId { get; init; }

    /// <example>Su orden #f1e2d3c4 fue confirmada.</example>
    [Required(ErrorMessage = "El mensaje es obligatorio.")]
    [MaxLength(500, ErrorMessage = "El mensaje no puede superar los 500 caracteres.")]
    public string Mensaje { get; init; } = string.Empty;

    /// <example>Email</example>
    [Required(ErrorMessage = "El tipo es obligatorio.")]
    [RegularExpression("^(Email|Push|SMS)$", ErrorMessage = "El tipo debe ser Email, Push o SMS.")]
    public string Tipo { get; init; } = string.Empty;
}