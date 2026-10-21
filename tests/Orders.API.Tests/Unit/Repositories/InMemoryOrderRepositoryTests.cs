using Orders.API.Models;
using Orders.API.Repositories;

namespace Orders.API.Tests.Unit.Repositories;

public class InMemoryOrderRepositoryTests
{
    private static readonly Guid Maria = Guid.NewGuid();
    private static readonly Guid Juan = Guid.NewGuid();
    private static readonly Guid Notebook = Guid.NewGuid();
    private static readonly Guid Auriculares = Guid.NewGuid();

    [Fact]
    public async Task GetByIdAsync_ConOrdenesIniciales_LaDevuelve()
    {
        var orden = CrearOrden(Maria, dia: 1, Notebook);
        var repository = new InMemoryOrderRepository([orden]);

        Assert.Same(orden, await repository.GetByIdAsync(orden.Id));
    }

    [Fact]
    public async Task GetByIdAsync_OrdenInexistente_DevuelveNull()
    {
        var repository = new InMemoryOrderRepository();

        Assert.Null(await repository.GetByIdAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task AddAsync_OrdenNueva_SePuedeObtener()
    {
        var repository = new InMemoryOrderRepository();
        var orden = CrearOrden(Maria, dia: 1, Notebook);

        await repository.AddAsync(orden);

        Assert.Same(orden, await repository.GetByIdAsync(orden.Id));
    }

    [Fact]
    public async Task UpdateAsync_OrdenExistente_GuardaLosCambios()
    {
        var orden = CrearOrden(Maria, dia: 1, Notebook);
        var repository = new InMemoryOrderRepository([orden]);
        var actualizada = CrearOrden(Maria, dia: 1, Notebook);
        actualizada.Id = orden.Id;
        actualizada.Estado = OrderStatus.Confirmada;

        await repository.UpdateAsync(actualizada);

        Assert.Equal(OrderStatus.Confirmada, (await repository.GetByIdAsync(orden.Id))!.Estado);
    }

    [Fact]
    public async Task GetAllAsync_SinFiltros_DevuelveTodasDeLaMasViejaALaMasNueva()
    {
        var nueva = CrearOrden(Maria, dia: 3, Notebook);
        var vieja = CrearOrden(Juan, dia: 1, Auriculares);
        var repository = new InMemoryOrderRepository([nueva, vieja]);

        var ordenes = await repository.GetAllAsync();

        Assert.Equal([vieja.Id, nueva.Id], ordenes.Select(orden => orden.Id));
    }

    [Fact]
    public async Task GetAllAsync_FiltroPorUsuario_DevuelveSoloLasDelUsuario()
    {
        var deMaria = CrearOrden(Maria, dia: 1, Notebook);
        var repository = new InMemoryOrderRepository([deMaria, CrearOrden(Juan, dia: 2, Notebook)]);

        var ordenes = await repository.GetAllAsync(usuarioId: Maria);

        Assert.Equal(deMaria.Id, Assert.Single(ordenes).Id);
    }

    [Fact]
    public async Task GetAllAsync_FiltroPorProducto_DevuelveLasQueIncluyenElProducto()
    {
        var conAuriculares = CrearOrden(Juan, dia: 2, Notebook, Auriculares);
        var repository = new InMemoryOrderRepository([CrearOrden(Maria, dia: 1, Notebook), conAuriculares]);

        var ordenes = await repository.GetAllAsync(productoId: Auriculares);

        Assert.Equal(conAuriculares.Id, Assert.Single(ordenes).Id);
    }

    [Fact]
    public async Task GetAllAsync_AmbosFiltros_AplicaLosDos()
    {
        var buscada = CrearOrden(Maria, dia: 2, Auriculares);
        var repository = new InMemoryOrderRepository(
        [
            CrearOrden(Maria, dia: 1, Notebook),
            buscada,
            CrearOrden(Juan, dia: 3, Auriculares)
        ]);

        var ordenes = await repository.GetAllAsync(usuarioId: Maria, productoId: Auriculares);

        Assert.Equal(buscada.Id, Assert.Single(ordenes).Id);
    }

    [Fact]
    public async Task GetAllAsync_SinCoincidencias_DevuelveListaVacia()
    {
        var repository = new InMemoryOrderRepository([CrearOrden(Maria, dia: 1, Notebook)]);

        Assert.Empty(await repository.GetAllAsync(productoId: Auriculares));
    }

    private static Order CrearOrden(Guid usuarioId, int dia, params Guid[] productos) => new()
    {
        Id = Guid.NewGuid(),
        UsuarioId = usuarioId,
        Items = productos
            .Select(productoId => new OrderItem { ProductoId = productoId, Cantidad = 1, PrecioUnitario = 100m })
            .ToList(),
        Total = productos.Length * 100m,
        Estado = OrderStatus.Pendiente,
        FechaCreacion = new DateTime(2024, 3, dia, 11, 0, 0, DateTimeKind.Utc),
        FechaActualizacion = new DateTime(2024, 3, dia, 11, 0, 0, DateTimeKind.Utc)
    };
}
