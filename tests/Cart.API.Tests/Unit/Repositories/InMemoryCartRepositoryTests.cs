using Cart.API.Models;
using Cart.API.Repositories;

namespace Cart.API.Tests.Unit.Repositories;

public class InMemoryCartRepositoryTests
{
    private static readonly Guid Usuario = Guid.NewGuid();

    [Fact]
    public async Task GetByUserIdAsync_ConCarritosIniciales_LosDevuelve()
    {
        var repository = new InMemoryCartRepository([CrearCarrito(Usuario)]);

        Assert.NotNull(await repository.GetByUserIdAsync(Usuario));
    }

    [Fact]
    public async Task GetByUserIdAsync_UsuarioSinCarrito_DevuelveNull()
    {
        var repository = new InMemoryCartRepository();

        Assert.Null(await repository.GetByUserIdAsync(Usuario));
    }

    [Fact]
    public async Task SaveAsync_CarritoNuevo_SePuedeObtener()
    {
        var repository = new InMemoryCartRepository();
        var carrito = CrearCarrito(Usuario);

        await repository.SaveAsync(carrito);

        Assert.Same(carrito, await repository.GetByUserIdAsync(Usuario));
    }

    [Fact]
    public async Task SaveAsync_CarritoExistente_LoReemplaza()
    {
        var repository = new InMemoryCartRepository([CrearCarrito(Usuario)]);
        var nuevo = CrearCarrito(Usuario);
        nuevo.AddItem(Guid.NewGuid(), 7);

        await repository.SaveAsync(nuevo);

        Assert.Same(nuevo, await repository.GetByUserIdAsync(Usuario));
    }

    [Fact]
    public async Task DeleteAsync_CarritoExistente_LoElimina()
    {
        var repository = new InMemoryCartRepository([CrearCarrito(Usuario)]);

        await repository.DeleteAsync(Usuario);

        Assert.Null(await repository.GetByUserIdAsync(Usuario));
    }

    private static ShoppingCart CrearCarrito(Guid usuarioId) => new()
    {
        UsuarioId = usuarioId,
        FechaActualizacion = new DateTime(2024, 3, 10, 10, 45, 0, DateTimeKind.Utc)
    };
}
