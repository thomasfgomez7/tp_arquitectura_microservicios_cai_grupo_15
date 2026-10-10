namespace Orders.API.DTOs;

/// <summary>
/// Un producto de la orden.
/// </summary>
public record OrderItemResponse
{
    /// <summary>ID del producto en Products.API.</summary>
    /// <example>3fa85f64-5717-4562-b3fc-2c963f66afa6</example>
    public required Guid ProductoId { get; init; }

    /// <summary>Unidades compradas.</summary>
    /// <example>2</example>
    public required int Cantidad { get; init; }

    /// <summary>Precio del producto al momento de crear la orden.</summary>
    /// <example>1500.00</example>
    public required decimal PrecioUnitario { get; init; }
}
