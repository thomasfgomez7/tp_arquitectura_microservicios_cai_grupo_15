using Microsoft.OpenApi;

namespace Notifications.API.Infrastructure;

/// <summary>
/// Documentación de la API con Swashbuckle (sección 5.1 del enunciado).
/// </summary>
public static class SwaggerExtensions
{
    public static IServiceCollection AddSwaggerDocumentation(this IServiceCollection services, IHostEnvironment environment)
    {
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "Notifications API",
                Version = "v1",
                Description = "Registro y envío simulado de notificaciones del E-Commerce. Valida el destinatario contra Users.API. " +
                              "Todas las respuestas de error siguen el contrato de la sección 3.1 del enunciado e incluyen " +
                              "errorCode, errorMessage y correlationId."
            });

            // XML comments de controllers y DTOs (se generan por GenerateDocumentationFile en el .csproj).
            options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, $"{environment.ApplicationName}.xml"));
            options.OperationFilter<ErrorExamplesOperationFilter>();
        });

        return services;
    }

    /// <summary>
    /// Swagger UI en /swagger. Queda habilitado en todos los entornos porque el enunciado lo pide
    /// en cada microservicio y la demo se hace desde ahí (D-22).
    /// </summary>
    public static WebApplication UseSwaggerDocumentation(this WebApplication app)
    {
        app.UseSwagger();
        app.UseSwaggerUI(options => options.DocumentTitle = "Notifications API");

        return app;
    }
}
