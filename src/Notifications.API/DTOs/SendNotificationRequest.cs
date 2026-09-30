using System.ComponentModel.DataAnnotations;

namespace Notifications.API.DTOs;

public record SendNotificationRequest
{
    [Required]
    public Guid UsuarioId { get; init; }

    [Required]
    [StringLength(500, ErrorMessage = "El mensaje no puede superar los 500 caracteres.")]
    public string Mensaje { get; init; } = string.Empty;

    [Required]
    [RegularExpression("^(Email|Push|SMS)$", ErrorMessage = "El tipo debe ser Email, Push o SMS.")]
    public string Tipo { get; init; } = string.Empty;
}