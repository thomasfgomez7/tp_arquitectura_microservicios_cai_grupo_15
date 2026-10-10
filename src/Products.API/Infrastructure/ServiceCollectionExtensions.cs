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
    // Si Orders.API no responde en este tiempo, el DELETE termina en un 500 con PRD-005 (D-39).
    // Mismo valor que los clientes de Orders y Notifications.
    private static readonly TimeSpan ClientTimeout = TimeSpan.FromSeconds(5);

    public static IServiceCollection AddProductServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<IProductRepository>(_ => new InMemoryProductRepository(ProductSeedData.Create()));
        services.AddScoped<IProductService, ProductService>();

        // Typed client: IHttpClientFactory crea y recicla los HttpClient (evita agotar conexiones) y le
        // inyecta a OrdersClient uno ya configurado. El DelegatingHandler propaga el Correlation ID.
        var ordersUrl = configuration["Services:OrdersApi:BaseUrl"]
                        ?? throw new InvalidOperationException("Falta la configuración 'Services:OrdersApi:BaseUrl'.");
        services.AddTransient<CorrelationIdDelegatingHandler>();
        services.AddHttpClient<IOrdersClient, OrdersClient>(client =>
            {
                client.BaseAddress = new Uri(ordersUrl);
                client.Timeout = ClientTimeout;
            })
            .AddHttpMessageHandler<CorrelationIdDelegatingHandler>();

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

        // La validación automática de [ApiController] lanza una ValidationException en lugar de
        // responder su propio 400, así todos los errores pasan por los mismos handlers.
        services.Configure<ApiBehaviorOptions>(options =>
            options.InvalidModelStateResponseFactory = context =>
                throw new Exceptions.ValidationException(ErrorCodes.PRD_002, ModelStateErrorMessage.Build(context.ModelState)));

        return services;
    }
}
