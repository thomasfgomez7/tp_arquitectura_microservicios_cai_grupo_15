namespace Orders.API.Clients;

/// <summary>
/// Datos de un producto que Orders lee de Products.API (GET /api/products/{id}).
/// Es un DTO propio de Orders: no se comparte código con Products (D-05).
/// </summary>
public record ProductInfo
{
    public required Guid Id { get; init; }

    public required string Nombre { get; init; }

    public required decimal Precio { get; init; }

    public required int Stock { get; init; }
}
