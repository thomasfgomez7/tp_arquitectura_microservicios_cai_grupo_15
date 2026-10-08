namespace Users.API.DTOs;

/// <summary>
/// Formato de email que usan las validaciones de los requests.
/// Se usa [RegularExpression] en lugar de [EmailAddress]: con un email vacío, [EmailAddress]
/// agrega un segundo error además del de [Required]. [RegularExpression] ignora los valores vacíos.
/// </summary>
public static class EmailFormat
{
    public const string Pattern = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";
}