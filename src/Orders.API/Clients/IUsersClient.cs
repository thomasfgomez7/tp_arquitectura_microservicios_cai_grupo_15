namespace Orders.API.Clients;

/// <summary>
/// Consulta a Users.API. El servicio de órdenes no sabe que del otro lado hay una llamada HTTP.
/// </summary>
public interface IUsersClient
{
    /// <summary>
    /// Devuelve el usuario, o null si Users.API responde 404.
    /// Si Users.API falla o no responde, lanza una excepción (termina en ORD-007, D-36).
    /// </summary>
    Task<UserInfo?> GetUserAsync(Guid userId, CancellationToken cancellationToken = default);
}
