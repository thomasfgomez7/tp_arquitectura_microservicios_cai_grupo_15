using Cart.API.Clients;
using Cart.API.ExceptionHandlers;
using Cart.API.Exceptions;
using Cart.API.Repositories;
using Cart.API.Services;
using Microsoft.AspNetCore.Mvc;

namespace Cart.API.Infrastructure;

/// <summary>
/// Único lugar que asocia cada interfaz con su implementación.
/// </summary>
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCartServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<ICartRepository>(_ => new InMemoryCartRepository(CartSeedData.Create()));
        services.AddScoped<ICartService, CartService>();

        // Typed client: IHttpClientFactory crea y recicla los HttpClient (evita agotar conexiones) y
        // le inyecta a ProductsClient uno ya configurado con la URL de Products.API.
        var productsUrl = configuration["Services:ProductsApi:BaseUrl"]
                          ?? throw new InvalidOperationException("Falta la configuración 'Services:ProductsApi:BaseUrl'.");
        services.AddHttpClient<IProductsClient, ProductsClient>(client => client.BaseAddress = new Uri(productsUrl));

        return services;
    }

    /// <summary>
    /// Correlation ID del request actual (sección 5.5 del enunciado).
    /// </summary>
    public static IServiceCollection AddCorrelationId(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();
        services.AddSingleton<ICorrelationIdAccessor, CorrelationIdAccessor>();

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

        // La validación automática de [ApiController] lanza una ValidationException con CRT-004 (D-25),
        // así todos los errores pasan por los mismos handlers.
        services.Configure<ApiBehaviorOptions>(options =>
            options.InvalidModelStateResponseFactory = context =>
                throw new Exceptions.ValidationException(ErrorCodes.CRT_004, ModelStateErrorMessage.Build(context.ModelState)));

        return services;
    }
}
