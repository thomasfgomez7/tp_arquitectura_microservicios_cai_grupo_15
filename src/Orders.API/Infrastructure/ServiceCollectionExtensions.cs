using Microsoft.AspNetCore.Mvc;
using Orders.API.Clients;
using Orders.API.ExceptionHandlers;
using Orders.API.Exceptions;
using Orders.API.Repositories;
using Orders.API.Services;

namespace Orders.API.Infrastructure;

/// <summary>
/// Único lugar que asocia cada interfaz con su implementación.
/// </summary>
public static class ServiceCollectionExtensions
{
    // D-36: si Users.API o Products.API no responden en este tiempo, la orden termina en un 500 con ORD-007.
    private static readonly TimeSpan ClientTimeout = TimeSpan.FromSeconds(5);

    public static IServiceCollection AddOrderServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<IOrderRepository, InMemoryOrderRepository>();
        services.AddScoped<IOrderService, OrderService>();

        // Typed clients: IHttpClientFactory crea y recicla los HttpClient (evita agotar conexiones) y
        // le inyecta a cada cliente uno ya configurado con la URL de su servicio.
        var usersUrl = configuration["Services:UsersApi:BaseUrl"]
                       ?? throw new InvalidOperationException("Falta la configuración 'Services:UsersApi:BaseUrl'.");
        services.AddHttpClient<IUsersClient, UsersClient>(client =>
        {
            client.BaseAddress = new Uri(usersUrl);
            client.Timeout = ClientTimeout;
        });

        var productsUrl = configuration["Services:ProductsApi:BaseUrl"]
                          ?? throw new InvalidOperationException("Falta la configuración 'Services:ProductsApi:BaseUrl'.");
        services.AddHttpClient<IProductsClient, ProductsClient>(client =>
        {
            client.BaseAddress = new Uri(productsUrl);
            client.Timeout = ClientTimeout;
        });

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

        // La validación automática de [ApiController] lanza una ValidationException con ORD-002,
        // así todos los errores pasan por los mismos handlers.
        services.Configure<ApiBehaviorOptions>(options =>
            options.InvalidModelStateResponseFactory = context =>
                throw new Exceptions.ValidationException(ErrorCodes.ORD_002, ModelStateErrorMessage.Build(context.ModelState)));

        return services;
    }
}
