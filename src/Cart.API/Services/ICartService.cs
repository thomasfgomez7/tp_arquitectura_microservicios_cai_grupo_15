using Cart.API.DTOs;

namespace Cart.API.Services;

/// <summary>
/// Reglas del carrito (CRT-001 a CRT-004).
/// </summary>
public interface ICartService
{
    Task<CartResponse> GetAsync(Guid usuarioId, CancellationToken cancellationToken = default);

    /// <summary>Agrega un producto; crea el carrito si el usuario no tenía uno.</summary>
    Task<CartResponse> AddItemAsync(Guid usuarioId, AddCartItemRequest request, CancellationToken cancellationToken = default);

    Task<CartResponse> UpdateItemAsync(Guid usuarioId, Guid productoId, UpdateCartItemRequest request, CancellationToken cancellationToken = default);

    Task RemoveItemAsync(Guid usuarioId, Guid productoId, CancellationToken cancellationToken = default);

    /// <summary>Vacía el carrito eliminándolo (D-27).</summary>
    Task ClearAsync(Guid usuarioId, CancellationToken cancellationToken = default);
}
