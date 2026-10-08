namespace Cart.API.DTOs;

/// <summary>
/// Un producto del carrito.
/// </summary>
public record CartItemResponse
{
    /// <summary>ID del producto en Products.API.</summary>
    /// <example>3fa85f64-5717-4562-b3fc-2c963f66afa6</example>
    public required Guid ProductoId { get; init; }

    /// <summary>Unidades en el carrito.</summary>
    /// <example>2</example>
    public required int Cantidad { get; init; }
}
