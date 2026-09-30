using Notifications.API.Clients;
using Notifications.API.DTOs;
using Notifications.API.Exceptions;
using Notifications.API.Models;
using Notifications.API.Repositories;

namespace Notifications.API.Services;

public class NotificationService(
    INotificationRepository repository,
    IUsersClient usersClient) : INotificationService
{
    public async Task<NotificationResponse> SendAsync(SendNotificationRequest request, CancellationToken cancellationToken = default)
    {
        if (!await usersClient.ExisteUsuarioAsync(request.UsuarioId, cancellationToken))
        {
            throw new NotFoundException(ErrorCodes.NTF_001, "El usuario destinatario no fue encontrado.");
        }

        var notification = new Notification
        {
            UsuarioId = request.UsuarioId,
            Mensaje = request.Mensaje,
            Tipo = request.Tipo,
            Estado = "Enviada" // El enunciado pide simular el envío exitoso
        };

        await repository.AgregarAsync(notification, cancellationToken);

        return new NotificationResponse
        {
            Id = notification.Id,
            UsuarioId = notification.UsuarioId,
            Mensaje = notification.Mensaje,
            Tipo = notification.Tipo,
            Estado = notification.Estado,
            FechaEnvio = notification.FechaEnvio
        };
    }

    public async Task<IEnumerable<NotificationResponse>> GetByUserIdAsync(Guid usuarioId, CancellationToken cancellationToken = default)
    {
        var notificaciones = await repository.ObtenerPorUsuarioAsync(usuarioId, cancellationToken);

        if (!notificaciones.Any())
        {
            throw new NotFoundException(ErrorCodes.NTF_003, "No se encontraron notificaciones para el usuario.");
        }

        return notificaciones.Select(n => new NotificationResponse
        {
            Id = n.Id,
            UsuarioId = n.UsuarioId,
            Mensaje = n.Mensaje,
            Tipo = n.Tipo,
            Estado = n.Estado,
            FechaEnvio = n.FechaEnvio
        });
    }
}