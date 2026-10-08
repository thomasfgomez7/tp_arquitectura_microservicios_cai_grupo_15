using Microsoft.AspNetCore.Identity;
using Users.API.DTOs;
using Users.API.Exceptions;
using Users.API.Models;
using Users.API.Repositories;

namespace Users.API.Services;

public class UserService(
    IUserRepository repository,
    IPasswordHasher<User> passwordHasher,
    AccountLockoutPolicy lockoutPolicy,
    TimeProvider timeProvider) : IUserService
{
    public async Task<UserResponse> RegisterAsync(
        RegisterUserRequest request,
        CancellationToken cancellationToken = default)
    {
        if (await repository.ExistsByEmailAsync(request.Email, cancellationToken))
        {
            throw new BusinessRuleException(
                ErrorCodes.USR_001,
                $"El email '{request.Email}' ya está registrado.",
                StatusCodes.Status409Conflict);
        }

        var newUser = new User
        {
            Id = Guid.NewGuid(),
            Nombre = request.Nombre,
            Apellido = request.Apellido,
            Email = request.Email,
            FechaRegistro = timeProvider.GetUtcNow().UtcDateTime,
            Activo = true,
            IntentosFallidos = 0
        };

        newUser.PasswordHash = passwordHasher.HashPassword(newUser, request.Password);

        await repository.AddAsync(newUser, cancellationToken);

        return ToUserResponse(newUser);
    }

    public async Task<LoginResponse> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken = default)
    {
        var user = await repository.GetByEmailAsync(request.Email, cancellationToken)
                   ?? throw InvalidCredentials();

        // Un usuario bloqueado no puede entrar, ni siquiera con la contraseña correcta (D-09).
        if (!user.Activo)
        {
            throw lockoutPolicy.IsLockedOutByAttempts(user)
                ? LockedOutByAttempts()
                : LockedOutManually();
        }

        var result = passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);

        if (result == PasswordVerificationResult.Failed)
        {
            lockoutPolicy.RegisterFailedAttempt(user);
            await repository.UpdateAsync(user, cancellationToken);

            // D-09: el tercer fallo bloquea la cuenta pero responde 401 USR-003;
            // el siguiente login responde 403 USR-004.
            throw InvalidCredentials();
        }

        lockoutPolicy.ResetFailedAttempts(user);
        await repository.UpdateAsync(user, cancellationToken);

        return new LoginResponse
        {
            Id = user.Id,
            Nombre = user.Nombre,
            Apellido = user.Apellido,
            Email = user.Email
        };
    }

    public async Task<UserResponse> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var user = await repository.GetByIdAsync(id, cancellationToken)
                   ?? throw new NotFoundException(ErrorCodes.USR_007, "Usuario no encontrado.");

        return ToUserResponse(user);
    }

    private static BusinessRuleException InvalidCredentials() =>
        new(ErrorCodes.USR_003,
            "Credenciales incorrectas.",
            StatusCodes.Status401Unauthorized);

    private static BusinessRuleException LockedOutByAttempts() =>
        new(ErrorCodes.USR_004,
            "Su cuenta fue bloqueada por superar el máximo de intentos fallidos. Contacte a soporte.",
            StatusCodes.Status403Forbidden);

    private static BusinessRuleException LockedOutManually() =>
        new(ErrorCodes.USR_005,
            "Su cuenta fue suspendida por razones de seguridad. Contacte a soporte.",
            StatusCodes.Status403Forbidden);

    private static UserResponse ToUserResponse(User user) => new()
    {
        Id = user.Id,
        Nombre = user.Nombre,
        Apellido = user.Apellido,
        Email = user.Email,
        FechaRegistro = user.FechaRegistro,
        Activo = user.Activo
    };
}