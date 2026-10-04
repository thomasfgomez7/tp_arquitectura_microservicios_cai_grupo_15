using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Users.API.ExceptionHandlers;
using Users.API.Exceptions;
using Users.API.Models;
using Users.API.Repositories;
using Users.API.Services;

namespace Users.API.Infrastructure;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddUserServices(this IServiceCollection services)
    {
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<IUserRepository, InMemoryUserRepository>();
        services.AddSingleton<IPasswordHasher<User>, PasswordHasher<User>>();
        services.AddTransient<AccountLockoutPolicy>();
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

        services.AddExceptionHandler<NotFoundExceptionHandler>();
        services.AddExceptionHandler<BusinessRuleExceptionHandler>();
        services.AddExceptionHandler<ValidationExceptionHandler>();
        services.AddExceptionHandler<GlobalExceptionHandler>();
        services.AddProblemDetails();

        services.Configure<ApiBehaviorOptions>(options =>
            options.InvalidModelStateResponseFactory = context =>
                throw new ValidationException(
                    ErrorCodes.USR_002,
                    ModelStateErrorMessage.Build(context.ModelState)));

        return services;
    }
}