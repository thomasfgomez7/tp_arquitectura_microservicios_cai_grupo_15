namespace Users.API.Models;

/// <summary>
/// Usuario del sistema (Apéndice A del enunciado).
/// Id y FechaRegistro los asigna UserService. PasswordHash nunca sale en una respuesta.
/// </summary>
public class User
{
    public Guid Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Apellido { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public DateTime FechaRegistro { get; set; }
    public bool Activo { get; set; }
    public int IntentosFallidos { get; set; }
}