using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Products.API.ExceptionHandlers;

/// <summary>
/// Convierte los errores de validación automática de [ApiController] en el errorMessage de PRD-002:
/// todos los problemas separados por "; " (sección 3.1 del enunciado).
/// </summary>
public static class ModelStateErrorMessage
{
    public const string InvalidJson = "El cuerpo de la solicitud no es un JSON válido.";

    public static string Build(ModelStateDictionary modelState)
    {
        // Las claves "$..." o vacías indican que el body no se pudo leer como JSON.
        // En ese caso los mensajes de .NET están en inglés y no aportan: se devuelve uno solo en español.
        if (modelState.Keys.Any(key => key.Length == 0 || key.StartsWith('$')))
        {
            return InvalidJson;
        }

        // Cada mensaje de las Data Annotations termina en punto: se quita para unirlos con "; "
        // y se agrega un único punto final ("A; B; C.").
        var messages = modelState.Values
            .SelectMany(entry => entry.Errors)
            .Select(error => error.ErrorMessage.TrimEnd('.'))
            .Distinct();

        return string.Join("; ", messages) + ".";
    }
}
