using Microsoft.AspNetCore.Mvc;
using Products.API.Clients;
using Products.API.ExceptionHandlers;
using Products.API.Exceptions;
using Products.API.Repositories;
using Products.API.Services;

namespace Products.API.Infrastructure;

/// <summary>
/// Único lugar que asocia cada interfaz con su implementación.
/// </summary>
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddProductServices(this IServiceCollection services)
    {
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<IProductRepository>(_ => new InMemoryProductRepository(ProductSeedData.Create()));
        services.AddSingleton<IOrdersClient, StubOrdersClient>();
        services.AddScoped<IProductService, ProductService>();

        return services;
    }

    /// <summary>
    /// Manejo global de errores con IExceptionHandler (sección 5.2 del enunciado).
    /// Los handlers se registran del más específico al más genérico: el framework usa el primero que devuelve true.
    /// </summary>
    public static IServiceCollection AddErrorHandling(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<ErrorHandlingOptions>(configuration.GetSection(ErrorHandlingOptions.SectionName));
        services.AddSingleton<ErrorResponseWriter>();

        services.AddExceptionHandler<NotFoundExceptionHandler>();
        services.AddExceptionHandler<BusinessRuleExceptionHandler>();
        services.AddExceptionHandler<ValidationExceptionHandler>();
        services.AddExceptionHandler<GlobalExceptionHandler>();
        services.AddProblemDetails();

        // La validación automática de [ApiController] lanza una ValidationException en lugar de
        // responder su propio 400, así todos los errores pasan por los mismos handlers.
        services.Configure<ApiBehaviorOptions>(options =>
            options.InvalidModelStateResponseFactory = context =>
                throw new Exceptions.ValidationException(ErrorCodes.PRD_002, ModelStateErrorMessage.Build(context.ModelState)));

        return services;
    }
}
