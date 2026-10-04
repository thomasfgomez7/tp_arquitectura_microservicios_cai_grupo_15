using Microsoft.AspNetCore.Identity;
using Users.API.DTOs;
using Users.API.Exceptions;
using Users.API.Models;
using Users.API.Repositories;

namespace Users.API.Services;

public class UserService(
    IUserRepository repository,
    IPasswordHasher<User> passwordHasher,
    AccountLockoutPolicy lockoutPolicy) : IUserService
{
    public async Task<UserResponse> RegisterAsync(
        RegisterUserRequest request,
        CancellationToken cancellationToken = default)
    {
        if (await repository.ExisteEmailAsync(request.Email, cancellationToken))
        {
            throw new BusinessRuleException(
                ErrorCodes.USR_001,
                $"El email '{request.Email}' ya está registrado.",
                409);
        }

        var newUser = new User
        {
            Nombre = request.Nombre,
            Apellido = request.Apellido,
            Email = request.Email
        };

        newUser.PasswordHash =
            passwordHasher.HashPassword(newUser, request.Password);

        await repository.AgregarAsync(newUser, cancellationToken);

        return ToUserResponse(newUser);
    }

    public async Task<LoginResponse> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken = default)
    {
        var user = await repository.ObtenerPorEmailAsync(
            request.Email,
            cancellationToken);

        if (user is null)
        {
            throw new BusinessRuleException(
                ErrorCodes.USR_003,
                "Credenciales incorrectas.",
                401);
        }

        if (!user.Activo)
        {
            if (user.IntentosFallidos >= 3)
            {
                throw new BusinessRuleException(
                    ErrorCodes.USR_004,
                    "Su cuenta fue bloqueada por superar el máximo de intentos fallidos. Contacte a soporte.",
                    403);
            }

            throw new BusinessRuleException(
                ErrorCodes.USR_005,
                "Su cuenta fue suspendida por razones de seguridad. Contacte a soporte.",
                403);
        }

        var result = passwordHasher.VerifyHashedPassword(
            user,
            user.PasswordHash,
            request.Password);

        if (result == PasswordVerificationResult.Failed)
        {
            lockoutPolicy.RegistrarIntentoFallido(user);
            await repository.ActualizarAsync(user, cancellationToken);

            // El tercer fallo bloquea la cuenta, pero responde 401 USR-003.
            // El siguiente login responderá 403 USR-004.
            throw new BusinessRuleException(
                ErrorCodes.USR_003,
                "Credenciales incorrectas.",
                401);
        }

        lockoutPolicy.ResetearIntentos(user);
        await repository.ActualizarAsync(user, cancellationToken);

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
        var user = await repository.ObtenerPorIdAsync(id, cancellationToken);

        if (user is null)
        {
            throw new NotFoundException(
                ErrorCodes.USR_007,
                "Usuario no encontrado.");
        }

        return ToUserResponse(user);
    }

    private static UserResponse ToUserResponse(User user)
    {
        return new UserResponse
        {
            Id = user.Id,
            Nombre = user.Nombre,
            Apellido = user.Apellido,
            Email = user.Email,
            FechaRegistro = user.FechaRegistro,
            Activo = user.Activo
        };
    }
}