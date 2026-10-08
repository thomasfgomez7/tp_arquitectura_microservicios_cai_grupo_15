using Microsoft.AspNetCore.Mvc;
using Notifications.API.Clients;
using Notifications.API.ExceptionHandlers;
using Notifications.API.Exceptions;
using Notifications.API.Repositories;
using Notifications.API.Services;

namespace Notifications.API.Infrastructure;

/// <summary>
/// Único lugar que asocia cada interfaz con su implementación.
/// </summary>
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddNotificationServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<INotificationRepository, InMemoryNotificationRepository>();
        services.AddSingleton<INotificationSender, SimulatedNotificationSender>();
        services.AddScoped<INotificationService, NotificationService>();

        // Typed client: IHttpClientFactory crea y recicla los HttpClient y le inyecta a UsersClient
        // uno ya configurado con la URL de Users.API.
        var usersUrl = configuration["Services:UsersApi:BaseUrl"]
                       ?? throw new InvalidOperationException("Falta la configuración 'Services:UsersApi:BaseUrl'.");

        services.AddHttpClient<IUsersClient, UsersClient>(client =>
        {
            client.BaseAddress = new Uri(usersUrl);
            client.Timeout = TimeSpan.FromSeconds(5);
        });

        return services;
    }

    public static IServiceCollection AddCorrelationId(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();
        services.AddSingleton<ICorrelationIdAccessor, CorrelationIdAccessor>();

        return services;
    }

    public static IServiceCollection AddErrorHandling(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<ErrorHandlingOptions>(
            configuration.GetSection(ErrorHandlingOptions.SectionName));

        services.AddSingleton<ErrorResponseWriter>();

        // Del más específico al más genérico: el framework usa el primero que devuelve true.
        services.AddExceptionHandler<NotFoundExceptionHandler>();
        services.AddExceptionHandler<BusinessRuleExceptionHandler>();
        services.AddExceptionHandler<ValidationExceptionHandler>();
        services.AddExceptionHandler<GlobalExceptionHandler>();
        services.AddProblemDetails();

        // Los errores automáticos de [ApiController] se convierten en NTF-002.
        services.Configure<ApiBehaviorOptions>(options =>
            options.InvalidModelStateResponseFactory = context =>
                throw new ValidationException(
                    ErrorCodes.NTF_002,
                    ModelStateErrorMessage.Build(context.ModelState)));

        return services;
    }
}