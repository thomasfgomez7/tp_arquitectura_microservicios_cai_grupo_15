using Products.API.Models;

namespace Products.API.DTOs;

public record ProductResponse(
    Guid Id,
    string Nombre,
    string? Descripcion,
    decimal Precio,
    int Stock,
    string Categoria,
    DateTime FechaCreacion)
{
    public static ProductResponse FromEntity(Product product) => new(
        product.Id,
        product.Nombre,
        product.Descripcion,
        product.Precio,
        product.Stock,
        product.Categoria,
        product.FechaCreacion);
}
