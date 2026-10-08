using Microsoft.AspNetCore.Mvc;
using Products.API.DTOs;
using Products.API.Exceptions;
using Products.API.Infrastructure;
using Products.API.Services;

namespace Products.API.Controllers;

/// <summary>
/// Catálogo de productos. Solo traduce HTTP ↔ DTO y delega en IProductService. Sin lógica de negocio
/// ni try/catch: las excepciones las convierten los IExceptionHandler.
/// </summary>
[ApiController]
[Route("api/products")]
[Tags("Products")]
[ProducesError(StatusCodes.Status500InternalServerError, ErrorCodes.PRD_005, "Error interno al procesar el producto.")]
public class ProductsController(IProductService productService) : ControllerBase
{
    private const string GetByIdRoute = "GetProductById";

    /// <summary>Lista los productos, con filtros opcionales.</summary>
    /// <param name="categoria">Filtra por categoría exacta, sin distinguir mayúsculas. Ej.: Electrónica.</param>
    /// <param name="nombre">Filtra por coincidencia parcial del nombre, sin distinguir mayúsculas. Ej.: notebook.</param>
    /// <param name="cancellationToken">Cancelación del request.</param>
    /// <response code="200">Lista de productos (vacía si ninguno coincide).</response>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<ProductResponse>>(StatusCodes.Status200OK, "application/json")]
    public async Task<ActionResult<IReadOnlyList<ProductResponse>>> GetAll(
        [FromQuery] string? categoria,
        [FromQuery] string? nombre,
        CancellationToken cancellationToken) =>
        Ok(await productService.GetAllAsync(categoria, nombre, cancellationToken));

    /// <summary>Obtiene un producto por su ID.</summary>
    /// <param name="id">ID del producto (GUID). Ej.: 3fa85f64-5717-4562-b3fc-2c963f66afa6.</param>
    /// <param name="cancellationToken">Cancelación del request.</param>
    /// <response code="200">El producto.</response>
    [HttpGet("{id}", Name = GetByIdRoute)]
    [ProducesResponseType<ProductResponse>(StatusCodes.Status200OK, "application/json")]
    [ProducesError(StatusCodes.Status404NotFound, ErrorCodes.PRD_001, "Producto no encontrado.")]
    public async Task<ActionResult<ProductResponse>> GetById(string id, CancellationToken cancellationToken) =>
        Ok(await productService.GetByIdAsync(ParseId(id), cancellationToken));

    /// <summary>Crea un producto nuevo.</summary>
    /// <remarks>El ID y la fecha de creación los asigna el servicio. No puede haber dos productos con el mismo nombre en la misma categoría.</remarks>
    /// <param name="request">Datos del producto.</param>
    /// <param name="cancellationToken">Cancelación del request.</param>
    /// <response code="201">Producto creado. El header Location apunta a GET /api/products/{id}.</response>
    [HttpPost]
    [Consumes("application/json")]
    [ProducesResponseType<ProductResponse>(StatusCodes.Status201Created, "application/json")]
    [ProducesError(StatusCodes.Status400BadRequest, ErrorCodes.PRD_002, "El nombre es obligatorio; El precio debe ser mayor a 0.")]
    [ProducesError(StatusCodes.Status409Conflict, ErrorCodes.PRD_003, "Ya existe un producto con ese nombre en la categoría 'Electrónica'.")]
    public async Task<ActionResult<ProductResponse>> Create(CreateProductRequest request, CancellationToken cancellationToken)
    {
        var product = await productService.CreateAsync(request, cancellationToken);

        return CreatedAtRoute(GetByIdRoute, new { id = product.Id }, product);
    }

    /// <summary>Actualiza todos los datos de un producto existente.</summary>
    /// <remarks>Conserva el ID y la fecha de creación.</remarks>
    /// <param name="id">ID del producto (GUID).</param>
    /// <param name="request">Datos nuevos del producto.</param>
    /// <param name="cancellationToken">Cancelación del request.</param>
    /// <response code="200">El producto actualizado.</response>
    [HttpPut("{id}")]
    [Consumes("application/json")]
    [ProducesResponseType<ProductResponse>(StatusCodes.Status200OK, "application/json")]
    [ProducesError(StatusCodes.Status400BadRequest, ErrorCodes.PRD_002, "El stock debe ser mayor o igual a 0.")]
    [ProducesError(StatusCodes.Status404NotFound, ErrorCodes.PRD_001, "Producto no encontrado.")]
    public async Task<ActionResult<ProductResponse>> Update(string id, UpdateProductRequest request, CancellationToken cancellationToken) =>
        Ok(await productService.UpdateAsync(ParseId(id), request, cancellationToken));

    /// <summary>Elimina un producto.</summary>
    /// <remarks>No se puede eliminar si figura en órdenes en estado Pendiente o Confirmada.</remarks>
    /// <param name="id">ID del producto (GUID).</param>
    /// <param name="cancellationToken">Cancelación del request.</param>
    /// <response code="204">Producto eliminado (sin body).</response>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesError(StatusCodes.Status404NotFound, ErrorCodes.PRD_001, "Producto no encontrado.")]
    [ProducesError(StatusCodes.Status409Conflict, ErrorCodes.PRD_004, "El producto tiene órdenes activas y no puede eliminarse.")]
    public async Task<IActionResult> Delete(string id, CancellationToken cancellationToken)
    {
        await productService.DeleteAsync(ParseId(id), cancellationToken);

        return NoContent();
    }

    // La ruta recibe el id como texto para que un id mal formado (ej. /api/products/99) responda
    // 404 con PRD-001, como pide el enunciado, en lugar de un 404 vacío del ruteo (D-17).
    private static Guid ParseId(string id) =>
        Guid.TryParse(id, out var guid)
            ? guid
            : throw new NotFoundException(ErrorCodes.PRD_001, "Producto no encontrado.");
}
