using Microsoft.AspNetCore.Mvc;
using Products.API.DTOs;
using Products.API.Exceptions;
using Products.API.Services;

namespace Products.API.Controllers;

/// <summary>
/// Solo traduce HTTP ↔ DTO y delega en IProductService. Sin lógica de negocio ni try/catch:
/// las excepciones las convierten los IExceptionHandler.
/// </summary>
[ApiController]
[Route("api/products")]
public class ProductsController(IProductService productService) : ControllerBase
{
    private const string GetByIdRoute = "GetProductById";

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ProductResponse>>> GetAll(
        [FromQuery] string? categoria,
        [FromQuery] string? nombre,
        CancellationToken cancellationToken) =>
        Ok(await productService.GetAllAsync(categoria, nombre, cancellationToken));

    [HttpGet("{id}", Name = GetByIdRoute)]
    public async Task<ActionResult<ProductResponse>> GetById(string id, CancellationToken cancellationToken) =>
        Ok(await productService.GetByIdAsync(ParseId(id), cancellationToken));

    [HttpPost]
    public async Task<ActionResult<ProductResponse>> Create(CreateProductRequest request, CancellationToken cancellationToken)
    {
        var product = await productService.CreateAsync(request, cancellationToken);

        return CreatedAtRoute(GetByIdRoute, new { id = product.Id }, product);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ProductResponse>> Update(string id, UpdateProductRequest request, CancellationToken cancellationToken) =>
        Ok(await productService.UpdateAsync(ParseId(id), request, cancellationToken));

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id, CancellationToken cancellationToken)
    {
        await productService.DeleteAsync(ParseId(id), cancellationToken);

        return NoContent();
    }

    // La ruta recibe el id como texto para que un id mal formado (ej. /api/products/99) responda
    // 404 con PRD-001, como pide el enunciado, en lugar de un 404 vacío del ruteo.
    private static Guid ParseId(string id) =>
        Guid.TryParse(id, out var guid)
            ? guid
            : throw new NotFoundException(ErrorCodes.PRD_001, "Producto no encontrado.");
}
