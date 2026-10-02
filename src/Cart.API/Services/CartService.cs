using Cart.API.Clients;
using Cart.API.DTOs;
using Cart.API.Exceptions;
using Cart.API.Models;
using Cart.API.Repositories;

namespace Cart.API.Services;

public class CartService(
    ICartRepository repository,
    IProductsClient productsClient,
    TimeProvider timeProvider) : ICartService
{
    public async Task<CartResponse> GetAsync(Guid usuarioId, CancellationToken cancellationToken = default)
    {
        var cart = await GetExistingCartAsync(usuarioId, cancellationToken);

        return CartResponse.FromEntity(cart);
    }

    public async Task<CartResponse> AddItemAsync(Guid usuarioId, AddCartItemRequest request, CancellationToken cancellationToken = default)
    {
        var productoId = request.ProductoId!.Value;
        var cart = await repository.GetByUserIdAsync(usuarioId, cancellationToken)
                   ?? new ShoppingCart { UsuarioId = usuarioId };

        // D-13: si el producto ya está en el carrito, el stock se valida contra el total.
        var cantidadTotal = cart.QuantityAfterAdding(productoId, request.Cantidad!.Value);
        await EnsureStockAsync(productoId, cantidadTotal, cancellationToken);

        cart.AddItem(productoId, request.Cantidad.Value);
        return await SaveAsync(cart, cancellationToken);
    }

    public async Task<CartResponse> UpdateItemAsync(Guid usuarioId, Guid productoId, UpdateCartItemRequest request, CancellationToken cancellationToken = default)
    {
        var cart = await GetExistingCartAsync(usuarioId, cancellationToken);
        var item = GetExistingItem(cart, productoId);

        // En PUT la cantidad se reemplaza: se valida la cantidad nueva, no la suma.
        await EnsureStockAsync(productoId, request.Cantidad!.Value, cancellationToken);

        item.Cantidad = request.Cantidad.Value;
        return await SaveAsync(cart, cancellationToken);
    }

    public async Task RemoveItemAsync(Guid usuarioId, Guid productoId, CancellationToken cancellationToken = default)
    {
        var cart = await GetExistingCartAsync(usuarioId, cancellationToken);
        GetExistingItem(cart, productoId);

        cart.RemoveItem(productoId);
        await SaveAsync(cart, cancellationToken);
    }

    public async Task ClearAsync(Guid usuarioId, CancellationToken cancellationToken = default)
    {
        await GetExistingCartAsync(usuarioId, cancellationToken);

        // D-27: vaciar elimina el carrito.
        await repository.DeleteAsync(usuarioId, cancellationToken);
    }

    private async Task<ShoppingCart> GetExistingCartAsync(Guid usuarioId, CancellationToken cancellationToken) =>
        await repository.GetByUserIdAsync(usuarioId, cancellationToken)
        ?? throw new NotFoundException(ErrorCodes.CRT_001, "Carrito no encontrado.");

    // D-26: un producto que no está en el carrito responde CRT-002 con un mensaje propio.
    private static CartItem GetExistingItem(ShoppingCart cart, Guid productoId) =>
        cart.FindItem(productoId)
        ?? throw new NotFoundException(ErrorCodes.CRT_002, "El producto no se encuentra en el carrito.");

    /// <summary>
    /// Consulta el producto en Products.API: CRT-002 si no existe, CRT-003 si no alcanza el stock.
    /// </summary>
    private async Task EnsureStockAsync(Guid productoId, int cantidadSolicitada, CancellationToken cancellationToken)
    {
        var product = await productsClient.GetProductAsync(productoId, cancellationToken)
                      ?? throw new NotFoundException(ErrorCodes.CRT_002, "Producto no encontrado.");

        if (cantidadSolicitada > product.Stock)
        {
            throw new BusinessRuleException(
                ErrorCodes.CRT_003,
                $"Stock insuficiente. Disponible: {product.Stock}, solicitado: {cantidadSolicitada}.",
                StatusCodes.Status422UnprocessableEntity);
        }
    }

    private async Task<CartResponse> SaveAsync(ShoppingCart cart, CancellationToken cancellationToken)
    {
        cart.FechaActualizacion = timeProvider.GetUtcNow().UtcDateTime;
        await repository.SaveAsync(cart, cancellationToken);

        return CartResponse.FromEntity(cart);
    }
}
