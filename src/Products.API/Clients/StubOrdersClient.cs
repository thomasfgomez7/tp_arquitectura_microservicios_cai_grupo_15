namespace Products.API.Clients;

/// <summary>
/// Implementación provisoria hasta que exista Orders.API: ningún producto tiene órdenes activas.
/// Se reemplaza por OrdersClient en la Etapa 9.
/// </summary>
public class StubOrdersClient : IOrdersClient
{
    public Task<bool> HasActiveOrdersAsync(Guid productId, CancellationToken cancellationToken = default) =>
        Task.FromResult(false);
}
