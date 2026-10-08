using Microsoft.AspNetCore.Mvc;
using Notifications.API.DTOs;
using Notifications.API.Exceptions;
using Notifications.API.Infrastructure;
using Notifications.API.Services;

namespace Notifications.API.Controllers;

/// <summary>
/// Notificaciones. Solo traduce HTTP ↔ DTO y delega en INotificationService. Sin lógica de negocio
/// ni try/catch: las excepciones las convierten los IExceptionHandler.
/// </summary>
[ApiController]
[Route("api/notifications")]
[Tags("Notifications")]
[ProducesError(StatusCodes.Status500InternalServerError, ErrorCodes.NTF_004, "Error interno al procesar la notificación.")]
public class NotificationsController(INotificationService notificationService) : ControllerBase
{
    /// <summary>Registra una notificación y simula su envío.</summary>
    /// <remarks>El usuario destinatario se verifica en Users.API (requiere Users.API levantado en el puerto 5002).</remarks>
    /// <param name="request">Destinatario, mensaje y tipo (Email, Push o SMS).</param>
    /// <param name="cancellationToken">Cancelación del request.</param>
    /// <response code="201">La notificación registrada, con su estado y fecha de envío.</response>
    [HttpPost("send")]
    [Consumes("application/json")]
    [ProducesResponseType<NotificationResponse>(StatusCodes.Status201Created, "application/json")]
    [ProducesError(StatusCodes.Status400BadRequest, ErrorCodes.NTF_002, "El tipo debe ser Email, Push o SMS.")]
    [ProducesError(StatusCodes.Status404NotFound, ErrorCodes.NTF_001, "El usuario destinatario no fue encontrado.")]
    public async Task<ActionResult<NotificationResponse>> Send(
        SendNotificationRequest request,
        CancellationToken cancellationToken) =>
        StatusCode(StatusCodes.Status201Created, await notificationService.SendAsync(request, cancellationToken));

    /// <summary>Lista las notificaciones de un usuario.</summary>
    /// <param name="userId">ID del usuario (GUID). Ej.: a1b2c3d4-0000-0000-0000-111122223333.</param>
    /// <param name="cancellationToken">Cancelación del request.</param>
    /// <response code="200">Las notificaciones del usuario, de la más vieja a la más nueva.</response>
    [HttpGet("{userId}")]
    [ProducesResponseType<IReadOnlyList<NotificationResponse>>(StatusCodes.Status200OK, "application/json")]
    [ProducesError(StatusCodes.Status404NotFound, ErrorCodes.NTF_003, "No se encontraron notificaciones para el usuario.")]
    public async Task<ActionResult<IReadOnlyList<NotificationResponse>>> GetByUserId(
        string userId,
        CancellationToken cancellationToken) =>
        Ok(await notificationService.GetByUserIdAsync(ParseUserId(userId), cancellationToken));

    // El id llega como texto para que uno mal formado (ej. /api/notifications/99) responda
    // 404 con su errorCode en lugar de un 404 vacío del ruteo (D-17).
    private static Guid ParseUserId(string userId) =>
        Guid.TryParse(userId, out var guid)
            ? guid
            : throw new NotFoundException(ErrorCodes.NTF_003, "No se encontraron notificaciones para el usuario.");
}
