using Products.API.Clients;
using Products.API.DTOs;
using Products.API.Exceptions;
using Products.API.Models;
using Products.API.Repositories;

namespace Products.API.Services;

public class ProductService(
    IProductRepository repository,
    IOrdersClient ordersClient,
    TimeProvider timeProvider) : IProductService
{
    public async Task<IReadOnlyList<ProductResponse>> GetAllAsync(string? categoria, string? nombre, CancellationToken cancellationToken = default)
    {
        var products = await repository.GetAllAsync(cancellationToken);

        return products
            .Where(p => string.IsNullOrWhiteSpace(categoria) || p.Categoria.Equals(categoria, StringComparison.OrdinalIgnoreCase))
            .Where(p => string.IsNullOrWhiteSpace(nombre) || p.Nombre.Contains(nombre, StringComparison.OrdinalIgnoreCase))
            .Select(ProductResponse.FromEntity)
            .ToList();
    }

    public async Task<ProductResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var product = await GetExistingProductAsync(id, cancellationToken);

        return ProductResponse.FromEntity(product);
    }

    public async Task<ProductResponse> CreateAsync(CreateProductRequest request, CancellationToken cancellationToken = default)
    {
        await EnsureNameIsUniqueInCategoryAsync(request.Nombre, request.Categoria, cancellationToken);

        var product = new Product
        {
            Id = Guid.NewGuid(),
            Nombre = request.Nombre,
            Descripcion = request.Descripcion,
            Precio = request.Precio!.Value,
            Stock = request.Stock!.Value,
            Categoria = request.Categoria,
            FechaCreacion = timeProvider.GetUtcNow().UtcDateTime
        };

        await repository.AddAsync(product, cancellationToken);

        return ProductResponse.FromEntity(product);
    }

    public async Task<ProductResponse> UpdateAsync(Guid id, UpdateProductRequest request, CancellationToken cancellationToken = default)
    {
        var product = await GetExistingProductAsync(id, cancellationToken);

        product.Nombre = request.Nombre;
        product.Descripcion = request.Descripcion;
        product.Precio = request.Precio!.Value;
        product.Stock = request.Stock!.Value;
        product.Categoria = request.Categoria;

        await repository.UpdateAsync(product, cancellationToken);

        return ProductResponse.FromEntity(product);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await GetExistingProductAsync(id, cancellationToken);

        if (await ordersClient.HasActiveOrdersAsync(id, cancellationToken))
        {
            throw new BusinessRuleException(
                ErrorCodes.PRD_004,
                "El producto tiene órdenes activas y no puede eliminarse.",
                StatusCodes.Status409Conflict);
        }

        await repository.DeleteAsync(id, cancellationToken);
    }

    private async Task<Product> GetExistingProductAsync(Guid id, CancellationToken cancellationToken) =>
        await repository.GetByIdAsync(id, cancellationToken)
        ?? throw new NotFoundException(ErrorCodes.PRD_001, "Producto no encontrado.");

    private async Task EnsureNameIsUniqueInCategoryAsync(string nombre, string categoria, CancellationToken cancellationToken)
    {
        var products = await repository.GetAllAsync(cancellationToken);

        var isDuplicate = products.Any(p =>
            p.Nombre.Equals(nombre, StringComparison.OrdinalIgnoreCase) &&
            p.Categoria.Equals(categoria, StringComparison.OrdinalIgnoreCase));

        if (isDuplicate)
        {
            throw new BusinessRuleException(
                ErrorCodes.PRD_003,
                $"Ya existe un producto con ese nombre en la categoría '{categoria}'.",
                StatusCodes.Status409Conflict);
        }
    }
}
