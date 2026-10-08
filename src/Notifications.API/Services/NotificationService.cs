using Notifications.API.Clients;
using Notifications.API.DTOs;
using Notifications.API.Exceptions;
using Notifications.API.Models;
using Notifications.API.Repositories;

namespace Notifications.API.Services;

public class NotificationService(
    INotificationRepository repository,
    IUsersClient usersClient,
    INotificationSender notificationSender) : INotificationService
{
    public async Task<NotificationResponse> SendAsync(
        SendNotificationRequest request,
        CancellationToken cancellationToken = default)
    {
        // [Required] ya lo valida antes de llegar acá; esto protege al servicio si lo usa otro código.
        var usuarioId = request.UsuarioId
            ?? throw new ValidationException(ErrorCodes.NTF_002, "El usuario es obligatorio.");

        if (await usersClient.GetUserAsync(usuarioId, cancellationToken) is null)
        {
            throw new NotFoundException(
                ErrorCodes.NTF_001,
                "El usuario destinatario no fue encontrado.");
        }

        var notification = new Notification
        {
            Id = Guid.NewGuid(),
            UsuarioId = usuarioId,
            Mensaje = request.Mensaje,
            Tipo = request.Tipo
        };

        var sendResult = await notificationSender.SendAsync(notification, cancellationToken);

        notification.Estado = sendResult.Estado;
        notification.FechaEnvio = sendResult.FechaEnvio;

        await repository.AgregarAsync(notification, cancellationToken);

        return ToResponse(notification);
    }

    public async Task<IEnumerable<NotificationResponse>> GetByUserIdAsync(
        Guid usuarioId,
        CancellationToken cancellationToken = default)
    {
        var notifications = await repository.ObtenerPorUsuarioAsync(usuarioId, cancellationToken);

        var result = notifications.Select(ToResponse).ToArray();

        if (result.Length == 0)
        {
            throw new NotFoundException(
                ErrorCodes.NTF_003,
                "No se encontraron notificaciones para el usuario.");
        }

        return result;
    }

    private static NotificationResponse ToResponse(Notification notification) => new()
    {
        Id = notification.Id,
        UsuarioId = notification.UsuarioId,
        Mensaje = notification.Mensaje,
        Tipo = notification.Tipo,
        Estado = notification.Estado,
        FechaEnvio = notification.FechaEnvio
    };
}