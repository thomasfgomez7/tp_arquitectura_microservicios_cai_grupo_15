using Microsoft.AspNetCore.Mvc;
using Notifications.API.DTOs;
using Notifications.API.Services;

namespace Notifications.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class NotificationsController(INotificationService notificationService) : ControllerBase
{
    [HttpPost("send")]
    public async Task<IActionResult> SendAsync([FromBody] SendNotificationRequest request, CancellationToken cancellationToken)
    {
        // Las validaciones de los DTOs (Data Annotations) se ejecutan solas antes de llegar acá.
        var response = await notificationService.SendAsync(request, cancellationToken);
        
        // El TP exige un HTTP 201 Created para envíos exitosos
        return StatusCode(StatusCodes.Status201Created, response);
    }

    [HttpGet("{userId:guid}")]
    public async Task<IActionResult> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken)
    {
        var response = await notificationService.GetByUserIdAsync(userId, cancellationToken);
        
        // Retorna HTTP 200 OK con la lista de notificaciones
        return Ok(response);
    }
}