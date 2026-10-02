using Cart.API.DTOs;
using Cart.API.Exceptions;
using Cart.API.Infrastructure;
using Cart.API.Services;
using Microsoft.AspNetCore.Mvc;

namespace Cart.API.Controllers;

/// <summary>
/// Carrito de compras. Solo traduce HTTP ↔ DTO y delega en ICartService. Sin lógica de negocio
/// ni try/catch: las excepciones las convierten los IExceptionHandler.
/// </summary>
[ApiController]
[Route("api/cart/{userId}")]
[Tags("Cart")]
[ProducesError(StatusCodes.Status500InternalServerError, ErrorCodes.CRT_005, "Error interno al procesar el carrito.")]
public class CartController(ICartService cartService) : ControllerBase
{
    /// <summary>Obtiene el carrito de un usuario.</summary>
    /// <param name="userId">ID del usuario (GUID). Ej.: a1b2c3d4-0000-0000-0000-111122223333.</param>
    /// <param name="cancellationToken">Cancelación del request.</param>
    /// <response code="200">El carrito con sus productos.</response>
    [HttpGet]
    [ProducesResponseType<CartResponse>(StatusCodes.Status200OK, "application/json")]
    [ProducesError(StatusCodes.Status404NotFound, ErrorCodes.CRT_001, "Carrito no encontrado.")]
    public async Task<ActionResult<CartResponse>> Get(string userId, CancellationToken cancellationToken) =>
        Ok(await cartService.GetAsync(ParseUserId(userId), cancellationToken));

    /// <summary>Agrega un producto al carrito.</summary>
    /// <remarks>
    /// Si el usuario no tiene carrito, se crea. Si el producto ya está en el carrito, se suma la cantidad y el
    /// stock se valida contra el total. El producto y su stock se consultan en Products.API.
    /// </remarks>
    /// <param name="userId">ID del usuario (GUID).</param>
    /// <param name="request">Producto y cantidad a agregar.</param>
    /// <param name="cancellationToken">Cancelación del request.</param>
    /// <response code="200">El carrito actualizado.</response>
    [HttpPost("items")]
    [Consumes("application/json")]
    [ProducesResponseType<CartResponse>(StatusCodes.Status200OK, "application/json")]
    [ProducesError(StatusCodes.Status400BadRequest, ErrorCodes.CRT_004, "La cantidad debe ser mayor a cero.")]
    [ProducesError(StatusCodes.Status404NotFound, ErrorCodes.CRT_002, "Producto no encontrado.")]
    [ProducesError(StatusCodes.Status422UnprocessableEntity, ErrorCodes.CRT_003, "Stock insuficiente. Disponible: 1, solicitado: 5.")]
    public async Task<ActionResult<CartResponse>> AddItem(string userId, AddCartItemRequest request, CancellationToken cancellationToken) =>
        Ok(await cartService.AddItemAsync(ParseUserId(userId), request, cancellationToken));

    /// <summary>Cambia la cantidad de un producto del carrito.</summary>
    /// <remarks>La cantidad nueva reemplaza a la anterior y se valida contra el stock de Products.API.</remarks>
    /// <param name="userId">ID del usuario (GUID).</param>
    /// <param name="productId">ID del producto (GUID).</param>
    /// <param name="request">Cantidad nueva.</param>
    /// <param name="cancellationToken">Cancelación del request.</param>
    /// <response code="200">El carrito actualizado.</response>
    [HttpPut("items/{productId}")]
    [Consumes("application/json")]
    [ProducesResponseType<CartResponse>(StatusCodes.Status200OK, "application/json")]
    [ProducesError(StatusCodes.Status400BadRequest, ErrorCodes.CRT_004, "La cantidad debe ser mayor a cero.")]
    [ProducesError(StatusCodes.Status404NotFound, ErrorCodes.CRT_001, "Carrito no encontrado.")]
    [ProducesError(StatusCodes.Status404NotFound, ErrorCodes.CRT_002, "El producto no se encuentra en el carrito.")]
    [ProducesError(StatusCodes.Status422UnprocessableEntity, ErrorCodes.CRT_003, "Stock insuficiente. Disponible: 2, solicitado: 4.")]
    public async Task<ActionResult<CartResponse>> UpdateItem(string userId, string productId, UpdateCartItemRequest request, CancellationToken cancellationToken) =>
        Ok(await cartService.UpdateItemAsync(ParseUserId(userId), ParseProductId(productId), request, cancellationToken));

    /// <summary>Quita un producto del carrito.</summary>
    /// <remarks>Si era el último producto, el carrito queda vacío (sigue existiendo).</remarks>
    /// <param name="userId">ID del usuario (GUID).</param>
    /// <param name="productId">ID del producto (GUID).</param>
    /// <param name="cancellationToken">Cancelación del request.</param>
    /// <response code="204">Producto quitado (sin body).</response>
    [HttpDelete("items/{productId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesError(StatusCodes.Status404NotFound, ErrorCodes.CRT_001, "Carrito no encontrado.")]
    [ProducesError(StatusCodes.Status404NotFound, ErrorCodes.CRT_002, "El producto no se encuentra en el carrito.")]
    public async Task<IActionResult> RemoveItem(string userId, string productId, CancellationToken cancellationToken)
    {
        await cartService.RemoveItemAsync(ParseUserId(userId), ParseProductId(productId), cancellationToken);

        return NoContent();
    }

    /// <summary>Vacía el carrito completo.</summary>
    /// <remarks>El carrito se elimina: un GET posterior responde CRT-001 y el próximo POST crea uno nuevo.</remarks>
    /// <param name="userId">ID del usuario (GUID).</param>
    /// <param name="cancellationToken">Cancelación del request.</param>
    /// <response code="204">Carrito vaciado (sin body).</response>
    [HttpDelete]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesError(StatusCodes.Status404NotFound, ErrorCodes.CRT_001, "Carrito no encontrado.")]
    public async Task<IActionResult> Clear(string userId, CancellationToken cancellationToken)
    {
        await cartService.ClearAsync(ParseUserId(userId), cancellationToken);

        return NoContent();
    }

    // Los ids llegan como texto para que uno mal formado responda 404 con su errorCode (D-17).
    private static Guid ParseUserId(string userId) =>
        Guid.TryParse(userId, out var guid)
            ? guid
            : throw new NotFoundException(ErrorCodes.CRT_001, "Carrito no encontrado.");

    private static Guid ParseProductId(string productId) =>
        Guid.TryParse(productId, out var guid)
            ? guid
            : throw new NotFoundException(ErrorCodes.CRT_002, "El producto no se encuentra en el carrito.");
}
