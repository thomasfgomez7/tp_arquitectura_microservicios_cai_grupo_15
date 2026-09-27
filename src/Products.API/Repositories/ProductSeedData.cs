using Products.API.Models;

namespace Products.API.Repositories;

/// <summary>
/// Productos precargados para la demo. Los IDs son fijos para poder usarlos desde Cart y Orders;
/// el primero es el del ejemplo del enunciado.
/// </summary>
public static class ProductSeedData
{
    public static IReadOnlyList<Product> Create() =>
    [
        new()
        {
            Id = Guid.Parse("3fa85f64-5717-4562-b3fc-2c963f66afa6"),
            Nombre = "Notebook Dell XPS 15",
            Descripcion = "Laptop 15 pulgadas, 32GB RAM",
            Precio = 1500.00m,
            Stock = 10,
            Categoria = "Electrónica",
            FechaCreacion = new DateTime(2024, 1, 15, 10, 30, 0, DateTimeKind.Utc)
        },
        new()
        {
            Id = Guid.Parse("aaaabbbb-cccc-dddd-eeee-ffff00001111"),
            Nombre = "Auriculares inalámbricos",
            Descripcion = "Auriculares bluetooth con cancelación de ruido",
            Precio = 350.00m,
            Stock = 25,
            Categoria = "Electrónica",
            FechaCreacion = new DateTime(2024, 1, 20, 9, 0, 0, DateTimeKind.Utc)
        },
        new()
        {
            Id = Guid.Parse("b2c3d4e5-0000-0000-0000-000000000003"),
            Nombre = "Remera de algodón",
            Descripcion = "Remera básica de algodón peinado",
            Precio = 25.00m,
            Stock = 100,
            Categoria = "Indumentaria",
            FechaCreacion = new DateTime(2024, 2, 1, 14, 0, 0, DateTimeKind.Utc)
        },
        new()
        {
            Id = Guid.Parse("b2c3d4e5-0000-0000-0000-000000000004"),
            Nombre = "Pelota de fútbol N°5",
            Descripcion = null,
            Precio = 40.00m,
            Stock = 50,
            Categoria = "Deportes",
            FechaCreacion = new DateTime(2024, 2, 10, 11, 15, 0, DateTimeKind.Utc)
        },
        new()
        {
            Id = Guid.Parse("b2c3d4e5-0000-0000-0000-000000000005"),
            Nombre = "Taladro percutor 750W",
            Descripcion = "Stock bajo para probar los errores de stock insuficiente",
            Precio = 120.00m,
            Stock = 2,
            Categoria = "Herramientas",
            FechaCreacion = new DateTime(2024, 3, 5, 16, 45, 0, DateTimeKind.Utc)
        }
    ];
}
