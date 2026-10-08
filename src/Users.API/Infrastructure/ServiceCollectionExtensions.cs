using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Users.API.ExceptionHandlers;
using Users.API.Exceptions;
using Users.API.Models;
using Users.API.Repositories;
using Users.API.Services;

namespace Users.API.Infrastructure;

/// <summary>
/// Único lugar que asocia cada interfaz con su implementación.
/// </summary>
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddUserServices(this IServiceCollection services)
    {
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<IPasswordHasher<User>, PasswordHasher<User>>();

        // El repositorio arranca con los usuarios de la demo; necesita el hasher para sus contraseñas.
        services.AddSingleton<IUserRepository>(provider =>
            new InMemoryUserRepository(
                UserSeedData.Create(provider.GetRequiredService<IPasswordHasher<User>>())));

        services.AddSingleton<AccountLockoutPolicy>();
        services.AddScoped<IUserService, UserService>();

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

        // Los errores automáticos de [ApiController] se convierten en USR-002.
        services.Configure<ApiBehaviorOptions>(options =>
            options.InvalidModelStateResponseFactory = context =>
                throw new ValidationException(
                    ErrorCodes.USR_002,
                    ModelStateErrorMessage.Build(context.ModelState)));

        return services;
    }
}