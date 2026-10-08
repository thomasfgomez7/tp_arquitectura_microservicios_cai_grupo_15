using Microsoft.AspNetCore.Mvc;
using Users.API.DTOs;
using Users.API.Exceptions;
using Users.API.Services;

namespace Users.API.Controllers;

/// <summary>
/// Usuarios: registro, login y consulta por ID. Solo traduce HTTP ↔ DTO y delega en IUserService.
/// Sin lógica de negocio ni try/catch: las excepciones las convierten los IExceptionHandler.
/// </summary>
[ApiController]
[Route("api/users")]
[Tags("Users")]
public class UsersController(IUserService userService) : ControllerBase
{
    private const string GetByIdRoute = "GetUserById";

    /// <summary>Registra un usuario nuevo.</summary>
    /// <remarks>El ID y la fecha de registro los asigna el servicio. La contraseña se guarda hasheada.</remarks>
    /// <param name="request">Datos del usuario.</param>
    /// <param name="cancellationToken">Cancelación del request.</param>
    /// <response code="201">Usuario creado. El header Location apunta a GET /api/users/{id}.</response>
    [HttpPost("register")]
    [Consumes("application/json")]
    [ProducesResponseType<UserResponse>(StatusCodes.Status201Created, "application/json")]
    public async Task<ActionResult<UserResponse>> Register(RegisterUserRequest request, CancellationToken cancellationToken)
    {
        var user = await userService.RegisterAsync(request, cancellationToken);

        return CreatedAtRoute(GetByIdRoute, new { id = user.Id }, user);
    }

    /// <summary>Autentica un usuario con email y contraseña.</summary>
    /// <remarks>Al tercer intento fallido consecutivo la cuenta se bloquea (D-09).</remarks>
    /// <param name="request">Email y contraseña.</param>
    /// <param name="cancellationToken">Cancelación del request.</param>
    /// <response code="200">Los datos del usuario autenticado.</response>
    [HttpPost("login")]
    [Consumes("application/json")]
    [ProducesResponseType<LoginResponse>(StatusCodes.Status200OK, "application/json")]
    public async Task<ActionResult<LoginResponse>> Login(LoginRequest request, CancellationToken cancellationToken) =>
        Ok(await userService.LoginAsync(request, cancellationToken));

    /// <summary>Obtiene un usuario por su ID (lo usan Orders y Notifications, D-06).</summary>
    /// <param name="id">ID del usuario (GUID). Ej.: a1b2c3d4-0000-0000-0000-111122223333.</param>
    /// <param name="cancellationToken">Cancelación del request.</param>
    /// <response code="200">El usuario.</response>
    [HttpGet("{id}", Name = GetByIdRoute)]
    [ProducesResponseType<UserResponse>(StatusCodes.Status200OK, "application/json")]
    public async Task<ActionResult<UserResponse>> GetById(string id, CancellationToken cancellationToken) =>
        Ok(await userService.GetByIdAsync(ParseId(id), cancellationToken));

    // El id llega como texto para que uno mal formado (ej. /api/users/99) responda
    // 404 con USR-007 en lugar de un 404 vacío del ruteo (D-17).
    private static Guid ParseId(string id) =>
        Guid.TryParse(id, out var guid)
            ? guid
            : throw new NotFoundException(ErrorCodes.USR_007, "Usuario no encontrado.");
}