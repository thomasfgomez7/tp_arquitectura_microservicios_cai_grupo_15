using System.ComponentModel.DataAnnotations;

namespace Users.API.DTOs;

public record RegisterUserRequest
{
    [Required]
    public string Nombre { get; init; } = string.Empty;
    
    [Required]
    public string Apellido { get; init; } = string.Empty;
    
    [Required]
    [EmailAddress]
    public string Email { get; init; } = string.Empty;
    
    [Required]
    public string Password { get; init; } = string.Empty;
}