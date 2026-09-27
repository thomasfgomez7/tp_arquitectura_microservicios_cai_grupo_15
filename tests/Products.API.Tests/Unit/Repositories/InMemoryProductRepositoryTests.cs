using Products.API.Models;
using Products.API.Repositories;

namespace Products.API.Tests.Unit.Repositories;

public class InMemoryProductRepositoryTests
{
    [Fact]
    public async Task GetAllAsync_ConProductosIniciales_DevuelveLosProductosIniciales()
    {
        var iniciales = new[] { CrearProducto("Notebook"), CrearProducto("Remera") };
        var repository = new InMemoryProductRepository(iniciales);

        var resultado = await repository.GetAllAsync();

        Assert.Equal(2, resultado.Count);
    }

    [Fact]
    public async Task AddAsync_ProductoNuevo_SePuedeObtenerPorId()
    {
        var repository = new InMemoryProductRepository();
        var producto = CrearProducto("Notebook");

        await repository.AddAsync(producto);

        Assert.Same(producto, await repository.GetByIdAsync(producto.Id));
    }

    [Fact]
    public async Task GetByIdAsync_IdInexistente_DevuelveNull()
    {
        var repository = new InMemoryProductRepository();

        Assert.Null(await repository.GetByIdAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task UpdateAsync_ProductoExistente_ReemplazaLosDatos()
    {
        var original = CrearProducto("Notebook");
        var repository = new InMemoryProductRepository([original]);
        var actualizado = CrearProducto("Notebook Pro");
        actualizado.Id = original.Id;

        await repository.UpdateAsync(actualizado);

        Assert.Equal("Notebook Pro", (await repository.GetByIdAsync(original.Id))!.Nombre);
    }

    [Fact]
    public async Task DeleteAsync_ProductoExistente_LoElimina()
    {
        var producto = CrearProducto("Notebook");
        var repository = new InMemoryProductRepository([producto]);

        await repository.DeleteAsync(producto.Id);

        Assert.Null(await repository.GetByIdAsync(producto.Id));
        Assert.Empty(await repository.GetAllAsync());
    }

    private static Product CrearProducto(string nombre) => new()
    {
        Id = Guid.NewGuid(),
        Nombre = nombre,
        Precio = 100.00m,
        Stock = 5,
        Categoria = "Otros",
        FechaCreacion = new DateTime(2024, 1, 15, 10, 30, 0, DateTimeKind.Utc)
    };
}
