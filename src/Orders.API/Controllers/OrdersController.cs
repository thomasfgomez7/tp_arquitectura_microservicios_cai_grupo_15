using Microsoft.AspNetCore.Mvc;
using Orders.API.DTOs;
using Orders.API.Exceptions;
using Orders.API.Infrastructure;
using Orders.API.Services;

namespace Orders.API.Controllers;

/// <summary>
/// Órdenes de compra. Solo traduce HTTP ↔ DTO y delega en IOrderService. Sin lógica de negocio
/// ni try/catch: las excepciones las convierten los IExceptionHandler.
/// </summary>
[ApiController]
[Route("api/orders")]
[Tags("Orders")]
[ProducesError(StatusCodes.Status500InternalServerError, ErrorCodes.ORD_007, "Error interno al procesar la orden.")]
public class OrdersController(IOrderService orderService) : ControllerBase
{
    private const string GetByIdRoute = "GetOrderById";

    /// <summary>Lista las órdenes, con filtros opcionales.</summary>
    /// <remarks>
    /// Sin filtros devuelve todas. Si los dos filtros vienen, se aplican juntos. Un filtro que no es un GUID
    /// no coincide con ninguna orden: devuelve una lista vacía, porque este endpoint solo responde 200 o 500 (D-37).
    /// </remarks>
    /// <param name="usuarioId">Filtra por usuario (GUID). Ej.: a1b2c3d4-0000-0000-0000-111122223333.</param>
    /// <param name="productoId">Filtra las órdenes que incluyen el producto (GUID, D-07). Ej.: 3fa85f64-5717-4562-b3fc-2c963f66afa6.</param>
    /// <param name="cancellationToken">Cancelación del request.</param>
    /// <response code="200">Lista de órdenes de la más vieja a la más nueva (vacía si ninguna coincide).</response>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<OrderResponse>>(StatusCodes.Status200OK, "application/json")]
    public async Task<ActionResult<IReadOnlyList<OrderResponse>>> GetAll(
        [FromQuery] string? usuarioId,
        [FromQuery] string? productoId,
        CancellationToken cancellationToken)
    {
        if (!TryParseFilter(usuarioId, out var usuario) || !TryParseFilter(productoId, out var producto))
        {
            return Ok(Array.Empty<OrderResponse>());
        }

        return Ok(await orderService.GetAllAsync(usuario, producto, cancellationToken));
    }

    /// <summary>Obtiene el detalle de una orden.</summary>
    /// <param name="id">ID de la orden (GUID). Ej.: f1e2d3c4-0000-0000-0000-aabbccddeeff.</param>
    /// <param name="cancellationToken">Cancelación del request.</param>
    /// <response code="200">La orden con sus items, total y estado.</response>
    [HttpGet("{id}", Name = GetByIdRoute)]
    [ProducesResponseType<OrderResponse>(StatusCodes.Status200OK, "application/json")]
    [ProducesError(StatusCodes.Status404NotFound, ErrorCodes.ORD_001, "Orden no encontrada.")]
    public async Task<ActionResult<OrderResponse>> GetById(string id, CancellationToken cancellationToken) =>
        Ok(await orderService.GetByIdAsync(ParseId(id), cancellationToken));

    /// <summary>Crea una orden nueva en estado Pendiente.</summary>
    /// <remarks>
    /// El usuario se verifica en Users.API (puerto 5002) y cada producto y su stock en Products.API (puerto 5001).
    /// El precio unitario se toma del producto y el total se calcula. Si un producto se repite, se suman las
    /// cantidades (D-34). Crear la orden no descuenta stock (D-14). El enunciado lista un 409 para este endpoint,
    /// pero el catálogo no tiene ningún error 409 al crear: no se devuelve (D-38).
    /// </remarks>
    /// <param name="request">Usuario y productos con sus cantidades.</param>
    /// <param name="cancellationToken">Cancelación del request.</param>
    /// <response code="201">Orden creada. El header Location apunta a GET /api/orders/{id}.</response>
    [HttpPost]
    [Consumes("application/json")]
    [ProducesResponseType<OrderResponse>(StatusCodes.Status201Created, "application/json")]
    [ProducesError(StatusCodes.Status400BadRequest, ErrorCodes.ORD_002, "La orden debe tener al menos un item.")]
    [ProducesError(StatusCodes.Status404NotFound, ErrorCodes.ORD_003, "Usuario no encontrado al crear la orden.")]
    [ProducesError(StatusCodes.Status404NotFound, ErrorCodes.ORD_004, "Producto no encontrado al crear la orden.")]
    [ProducesError(StatusCodes.Status422UnprocessableEntity, ErrorCodes.ORD_005, "Stock insuficiente para 'Notebook Dell XPS 15'. Disponible: 2, solicitado: 5.")]
    public async Task<ActionResult<OrderResponse>> Create(CreateOrderRequest request, CancellationToken cancellationToken)
    {
        var order = await orderService.CreateAsync(request, cancellationToken);

        return CreatedAtRoute(GetByIdRoute, new { id = order.Id }, order);
    }

    /// <summary>Cambia el estado de una orden.</summary>
    /// <remarks>
    /// Transiciones permitidas: Pendiente → Confirmada → Enviada → Entregada, y Pendiente o Confirmada → Cancelada.
    /// Entregada y Cancelada son estados finales.
    /// </remarks>
    /// <param name="id">ID de la orden (GUID).</param>
    /// <param name="request">Estado nuevo.</param>
    /// <param name="cancellationToken">Cancelación del request.</param>
    /// <response code="200">El estado nuevo y la fecha del cambio.</response>
    [HttpPut("{id}/status")]
    [Consumes("application/json")]
    [ProducesResponseType<OrderStatusResponse>(StatusCodes.Status200OK, "application/json")]
    [ProducesError(StatusCodes.Status400BadRequest, ErrorCodes.ORD_002, "El estado debe ser Pendiente, Confirmada, Enviada, Entregada o Cancelada.")]
    [ProducesError(StatusCodes.Status404NotFound, ErrorCodes.ORD_001, "Orden no encontrada.")]
    [ProducesError(StatusCodes.Status409Conflict, ErrorCodes.ORD_006, "Una orden en estado 'Entregada' no puede pasar a 'Pendiente'.")]
    public async Task<ActionResult<OrderStatusResponse>> UpdateStatus(
        string id,
        UpdateOrderStatusRequest request,
        CancellationToken cancellationToken) =>
        Ok(await orderService.UpdateStatusAsync(ParseId(id), request, cancellationToken));

    // La ruta recibe el id como texto para que uno mal formado (ej. /api/orders/99) responda
    // 404 con ORD-001 en lugar de un 404 vacío del ruteo (D-17).
    private static Guid ParseId(string id) =>
        Guid.TryParse(id, out var guid)
            ? guid
            : throw new NotFoundException(ErrorCodes.ORD_001, "Orden no encontrada.");

    // Un filtro vacío no filtra; uno que no es GUID no coincide con nada (D-37).
    private static bool TryParseFilter(string? value, out Guid? guid)
    {
        guid = null;
        if (string.IsNullOrEmpty(value))
        {
            return true;
        }

        if (!Guid.TryParse(value, out var parsed))
        {
            return false;
        }

        guid = parsed;
        return true;
    }
}
