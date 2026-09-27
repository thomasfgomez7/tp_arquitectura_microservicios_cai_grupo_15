namespace Products.API.Clients;

/// <summary>
/// Consulta a Orders.API. El servicio de productos no sabe que del otro lado hay una llamada HTTP.
/// </summary>
public interface IOrdersClient
{
    /// <summary>
    /// Indica si el producto figura en alguna orden en estado Pendiente o Confirmada (PRD-004).
    /// </summary>
    Task<bool> HasActiveOrdersAsync(Guid productId, CancellationToken cancellationToken = default);
}
