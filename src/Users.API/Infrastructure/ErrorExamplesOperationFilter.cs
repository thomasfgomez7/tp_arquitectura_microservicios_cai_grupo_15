using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;
using Users.API.ExceptionHandlers;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Users.API.Infrastructure;

/// <summary>
/// Agrega a cada respuesta de error de Swagger un ejemplo por cada [ProducesError] del endpoint
/// (sección 5.1 del enunciado). El JSON lo arma ErrorResponseWriter, el mismo que arma las respuestas reales.
/// </summary>
public partial class ErrorExamplesOperationFilter : IOperationFilter
{
    private const string ExampleCorrelationId = "0f8fad5b-d9cb-469f-a165-70867728950e";

    // Valores de ejemplo para los parámetros de ruta, tomados de los ejemplos del enunciado.
    private static readonly Dictionary<string, string> ExampleRouteValues = new()
    {
        ["id"] = "a1b2c3d4-0000-0000-0000-111122223333"
    };

    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var errors = context.MethodInfo.GetCustomAttributes<ProducesErrorAttribute>()
            .Concat(context.MethodInfo.DeclaringType?.GetCustomAttributes<ProducesErrorAttribute>() ?? []);

        var instance = "/" + RouteParameter().Replace(
            context.ApiDescription.RelativePath ?? string.Empty,
            match => ExampleRouteValues.GetValueOrDefault(match.Groups[1].Value, match.Value));

        foreach (var group in errors.GroupBy(e => e.StatusCode))
        {
            if (operation.Responses is null
                || !operation.Responses.TryGetValue(group.Key.ToString(), out var response)
                || response is not OpenApiResponse openApiResponse
                || openApiResponse.Content is null
                || !openApiResponse.Content.TryGetValue(ErrorResponseWriter.ContentType, out var mediaType))
            {
                continue;
            }

            openApiResponse.Description = string.Join(" · ", group.Select(e => $"{e.ErrorCode}: {e.ErrorMessage}"));
            mediaType.Examples ??= new Dictionary<string, IOpenApiExample>();

            foreach (var error in group)
            {
                var example = ErrorResponseWriter.Build(error.StatusCode, error.ErrorCode, error.ErrorMessage, instance, ExampleCorrelationId);
                mediaType.Examples[error.ErrorCode] = new OpenApiExample
                {
                    Summary = $"{error.ErrorCode} — {error.ErrorMessage}",
                    Value = JsonSerializer.SerializeToNode(example, JsonSerializerOptions.Web)
                };
            }
        }
    }

    [GeneratedRegex(@"\{(\w+)\}")]
    private static partial Regex RouteParameter();
}
