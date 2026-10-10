namespace Products.API.Clients;

/// <summary>
/// Datos de una orden que Products lee de Orders.API (GET /api/orders?productoId=).
/// Es un DTO propio de Products: no se comparte código con Orders (D-05). Solo tiene lo que Products usa.
/// </summary>
public record OrderInfo
{
    public required Guid Id { get; init; }

    public required string Estado { get; init; }

    /// <summary>
    /// Una orden Pendiente o Confirmada impide borrar sus productos (PRD-004, sección 4.1 del enunciado).
    /// </summary>
    public bool EstaActiva => Estado is "Pendiente" or "Confirmada";
}
