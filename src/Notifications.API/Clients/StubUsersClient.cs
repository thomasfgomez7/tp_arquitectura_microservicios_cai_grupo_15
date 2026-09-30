namespace Notifications.API.Clients;

public class StubUsersClient : IUsersClient
{
    public Task<bool> ExisteUsuarioAsync(Guid usuarioId, CancellationToken cancellationToken = default)
    {
        // Simulamos que el usuario existe si su Guid no está vacío
        return Task.FromResult(usuarioId != Guid.Empty);
    }
}