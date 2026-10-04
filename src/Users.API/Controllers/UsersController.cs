using Microsoft.AspNetCore.Mvc;
using Users.API.DTOs;
using Users.API.Exceptions;
using Users.API.Services;

namespace Users.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController(IUserService userService) : ControllerBase
{
    [HttpPost("register")]
    public async Task<IActionResult> RegisterAsync(
        [FromBody] RegisterUserRequest request,
        CancellationToken cancellationToken)
    {
        var response = await userService.RegisterAsync(
            request,
            cancellationToken);

        return Created($"/api/users/{response.Id}", response);
    }

    [HttpPost("login")]
    public async Task<IActionResult> LoginAsync(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken)
    {
        var response = await userService.LoginAsync(
            request,
            cancellationToken);

        return Ok(response);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetByIdAsync(
        string id,
        CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(id, out var userId))
        {
            throw new NotFoundException(
                ErrorCodes.USR_007,
                "Usuario no encontrado.");
        }

        var response = await userService.GetByIdAsync(
            userId,
            cancellationToken);

        return Ok(response);
    }
}