using Cart.API.Models;

namespace Cart.API.Repositories;

/// <summary>
/// Carrito precargado para la demo: el del ejemplo del enunciado (usuario María González, con la notebook
/// y los auriculares de los datos semilla de Products.API).
/// </summary>
public static class CartSeedData
{
    public static IReadOnlyList<ShoppingCart> Create() =>
    [
        new()
        {
            UsuarioId = Guid.Parse("a1b2c3d4-0000-0000-0000-111122223333"),
            Items =
            [
                new CartItem { ProductoId = Guid.Parse("3fa85f64-5717-4562-b3fc-2c963f66afa6"), Cantidad = 1 },
                new CartItem { ProductoId = Guid.Parse("aaaabbbb-cccc-dddd-eeee-ffff00001111"), Cantidad = 3 }
            ],
            FechaActualizacion = new DateTime(2024, 3, 10, 10, 45, 0, DateTimeKind.Utc)
        }
    ];
}
