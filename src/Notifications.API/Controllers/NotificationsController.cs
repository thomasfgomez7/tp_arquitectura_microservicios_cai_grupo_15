using Microsoft.AspNetCore.Mvc;
using Notifications.API.DTOs;
using Notifications.API.Exceptions;
using Notifications.API.Services;

namespace Notifications.API.Controllers;

/// <summary>
/// Notificaciones. Solo traduce HTTP ↔ DTO y delega en INotificationService. Sin lógica de negocio
/// ni try/catch: las excepciones las convierten los IExceptionHandler.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class NotificationsController(INotificationService notificationService) : ControllerBase
{
    [HttpPost("send")]
    public async Task<IActionResult> SendAsync(
        [FromBody] SendNotificationRequest request,
        CancellationToken cancellationToken)
    {
        var response = await notificationService.SendAsync(request, cancellationToken);

        return StatusCode(StatusCodes.Status201Created, response);
    }

    // El id llega como texto para que uno mal formado responda 404 con su errorCode (D-17).
    [HttpGet("{userId}")]
    public async Task<IActionResult> GetByUserIdAsync(string userId, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(userId, out var usuarioId))
        {
            throw new NotFoundException(
                ErrorCodes.NTF_003,
                "No se encontraron notificaciones para el usuario.");
        }

        var response = await notificationService.GetByUserIdAsync(usuarioId, cancellationToken);

        return Ok(response);
    }
}