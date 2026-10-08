using System.Reflection;
using System.Text.Json;
using Microsoft.OpenApi;
using Products.API.ExceptionHandlers;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Products.API.Infrastructure;

/// <summary>
/// Agrega a cada respuesta de error de Swagger un ejemplo por cada [ProducesError] del endpoint
/// (sección 5.1 del enunciado). El JSON lo arma ErrorResponseWriter, el mismo que arma las respuestas reales.
/// </summary>
public class ErrorExamplesOperationFilter : IOperationFilter
{
    private const string ExampleId = "3fa85f64-5717-4562-b3fc-2c963f66afa6";
    private const string ExampleCorrelationId = "0f8fad5b-d9cb-469f-a165-70867728950e";

    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var errors = context.MethodInfo.GetCustomAttributes<ProducesErrorAttribute>()
            .Concat(context.MethodInfo.DeclaringType?.GetCustomAttributes<ProducesErrorAttribute>() ?? []);

        var instance = "/" + (context.ApiDescription.RelativePath ?? string.Empty).Replace("{id}", ExampleId);

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
}
