using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Users.API.ExceptionHandlers;

public static class ModelStateErrorMessage
{
    public const string InvalidJson =
        "El cuerpo de la solicitud no es un JSON válido.";

    public static string Build(ModelStateDictionary modelState)
    {
        if (modelState.Keys.Any(key => key.Length == 0 || key.StartsWith('$')))
        {
            return InvalidJson;
        }

        var messages = modelState.Values
            .SelectMany(entry => entry.Errors)
            .Select(error => error.ErrorMessage.TrimEnd('.'))
            .Where(message => !string.IsNullOrWhiteSpace(message))
            .Distinct()
            .ToArray();

        return messages.Length == 0
            ? "Los datos enviados no son válidos."
            : string.Join("; ", messages) + ".";
    }
}