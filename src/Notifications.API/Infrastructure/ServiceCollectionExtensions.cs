using Microsoft.AspNetCore.Mvc;
using Notifications.API.Clients;
using Notifications.API.ExceptionHandlers;
using Notifications.API.Exceptions;
using Notifications.API.Repositories;
using Notifications.API.Services;

namespace Notifications.API.Infrastructure;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddNotificationServices(
        this IServiceCollection services)
    {
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<INotificationRepository, InMemoryNotificationRepository>();
        services.AddScoped<IUsersClient, StubUsersClient>();
        services.AddSingleton<INotificationSender, SimulatedNotificationSender>();
        services.AddScoped<INotificationService, NotificationService>();

        return services;
    }

    public static IServiceCollection AddCorrelationId(
        this IServiceCollection services)
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

        // Se registran desde la excepción más específica hasta la más general.
        services.AddExceptionHandler<NotFoundExceptionHandler>();
        services.AddExceptionHandler<BusinessRuleExceptionHandler>();
        services.AddExceptionHandler<ValidationExceptionHandler>();
        services.AddExceptionHandler<GlobalExceptionHandler>();
        services.AddProblemDetails();

        // Convierte los errores automáticos de [ApiController] en NTF-002.
        services.Configure<ApiBehaviorOptions>(options =>
            options.InvalidModelStateResponseFactory = context =>
                throw new ValidationException(
                    ErrorCodes.NTF_002,
                    ModelStateErrorMessage.Build(context.ModelState)));

        return services;
    }
}