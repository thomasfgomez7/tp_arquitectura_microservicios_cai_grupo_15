namespace Notifications.API.Clients;

public interface IUsersClient
{
    Task<bool> ExisteUsuarioAsync(Guid usuarioId, CancellationToken cancellationToken = default);
}