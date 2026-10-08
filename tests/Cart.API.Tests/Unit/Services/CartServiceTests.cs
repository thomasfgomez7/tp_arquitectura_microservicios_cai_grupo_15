using Cart.API.Clients;
using Cart.API.DTOs;
using Cart.API.Exceptions;
using Cart.API.Models;
using Cart.API.Repositories;
using Cart.API.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;

namespace Cart.API.Tests.Unit.Services;

/// <summary>
/// El repositorio es el real en memoria (funciona como un "fake": guarda de verdad, sin red ni disco),
/// porque los casos encadenan operaciones sobre el mismo carrito. Products.API se reemplaza por un doble.
/// </summary>
public class CartServiceTests
{
    private static readonly DateTimeOffset Ahora = new(2026, 10, 1, 9, 0, 0, TimeSpan.Zero);
    private static readonly Guid Usuario = Guid.NewGuid();
    private static readonly Guid Notebook = Guid.NewGuid();
    private static readonly Guid Auriculares = Guid.NewGuid();

    private readonly InMemoryCartRepository _repository = new();
    private readonly IProductsClient _productsClient = Substitute.For<IProductsClient>();
    private readonly CartService _service;

    public CartServiceTests()
    {
        _service = new CartService(_repository, _productsClient, new FakeTimeProvider(Ahora));
        DadoElProducto(Notebook, stock: 10);
        DadoElProducto(Auriculares, stock: 3);
    }

    // ---------- GetAsync ----------

    [Fact]
    public async Task GetAsync_CarritoExistente_DevuelveSusItems()
    {
        await DadoElCarrito((Notebook, 1), (Auriculares, 3));

        var carrito = await _service.GetAsync(Usuario);

        Assert.Equal(Usuario, carrito.UsuarioId);
        Assert.Equal(2, carrito.Items.Count);
    }

    [Fact]
    public async Task GetAsync_UsuarioSinCarrito_LanzaNotFoundConCRT001()
    {
        var excepcion = await Assert.ThrowsAsync<NotFoundException>(() => _service.GetAsync(Usuario));

        Assert.Equal(ErrorCodes.CRT_001, excepcion.ErrorCode);
        Assert.Equal("Carrito no encontrado.", excepcion.Message);
    }

    // ---------- AddItemAsync ----------

    [Fact]
    public async Task AddItemAsync_UsuarioSinCarrito_CreaElCarritoConElProducto()
    {
        var carrito = await _service.AddItemAsync(Usuario, Agregar(Notebook, 2));

        var item = Assert.Single(carrito.Items);
        Assert.Equal(Notebook, item.ProductoId);
        Assert.Equal(2, item.Cantidad);
        Assert.Equal(Ahora.UtcDateTime, carrito.FechaActualizacion);
        Assert.NotNull(await _repository.GetByUserIdAsync(Usuario));
    }

    [Fact]
    public async Task AddItemAsync_ProductoYaEnElCarrito_SumaLaCantidad()
    {
        await DadoElCarrito((Notebook, 2));

        var carrito = await _service.AddItemAsync(Usuario, Agregar(Notebook, 3));

        Assert.Equal(5, Assert.Single(carrito.Items).Cantidad);
    }

    [Fact]
    public async Task AddItemAsync_ActualizaLaFechaDelCarrito()
    {
        await DadoElCarrito((Notebook, 1));

        var carrito = await _service.AddItemAsync(Usuario, Agregar(Auriculares, 1));

        Assert.Equal(Ahora.UtcDateTime, carrito.FechaActualizacion);
    }

    [Fact]
    public async Task AddItemAsync_ProductoInexistenteEnProducts_LanzaNotFoundConCRT002YNoCreaElCarrito()
    {
        var excepcion = await Assert.ThrowsAsync<NotFoundException>(
            () => _service.AddItemAsync(Usuario, Agregar(Guid.NewGuid(), 1)));

        Assert.Equal(ErrorCodes.CRT_002, excepcion.ErrorCode);
        Assert.Equal("Producto no encontrado.", excepcion.Message);
        Assert.Null(await _repository.GetByUserIdAsync(Usuario));
    }

    [Fact]
    public async Task AddItemAsync_StockInsuficiente_LanzaBusinessRuleConCRT003Y422()
    {
        var excepcion = await Assert.ThrowsAsync<BusinessRuleException>(
            () => _service.AddItemAsync(Usuario, Agregar(Auriculares, 5)));

        Assert.Equal(ErrorCodes.CRT_003, excepcion.ErrorCode);
        Assert.Equal(StatusCodes.Status422UnprocessableEntity, excepcion.StatusCode);
        Assert.Equal("Stock insuficiente. Disponible: 3, solicitado: 5.", excepcion.Message);
    }

    [Fact]
    public async Task AddItemAsync_LaSumaSuperaElStock_LanzaCRT003ConLaCantidadTotal()
    {
        // D-13: el stock se valida contra el total que quedaría en el carrito.
        await DadoElCarrito((Auriculares, 2));

        var excepcion = await Assert.ThrowsAsync<BusinessRuleException>(
            () => _service.AddItemAsync(Usuario, Agregar(Auriculares, 2)));

        Assert.Equal("Stock insuficiente. Disponible: 3, solicitado: 4.", excepcion.Message);
        Assert.Equal(2, (await _repository.GetByUserIdAsync(Usuario))!.FindItem(Auriculares)!.Cantidad);
    }

    // ---------- UpdateItemAsync ----------

    [Fact]
    public async Task UpdateItemAsync_ProductoEnElCarrito_ReemplazaLaCantidad()
    {
        await DadoElCarrito((Notebook, 1));

        var carrito = await _service.UpdateItemAsync(Usuario, Notebook, Cantidad(4));

        Assert.Equal(4, Assert.Single(carrito.Items).Cantidad);
        Assert.Equal(Ahora.UtcDateTime, carrito.FechaActualizacion);
    }

    [Fact]
    public async Task UpdateItemAsync_UsuarioSinCarrito_LanzaNotFoundConCRT001()
    {
        var excepcion = await Assert.ThrowsAsync<NotFoundException>(
            () => _service.UpdateItemAsync(Usuario, Notebook, Cantidad(1)));

        Assert.Equal(ErrorCodes.CRT_001, excepcion.ErrorCode);
    }

    [Fact]
    public async Task UpdateItemAsync_ProductoQueNoEstaEnElCarrito_LanzaNotFoundConCRT002()
    {
        await DadoElCarrito((Notebook, 1));

        var excepcion = await Assert.ThrowsAsync<NotFoundException>(
            () => _service.UpdateItemAsync(Usuario, Auriculares, Cantidad(1)));

        Assert.Equal(ErrorCodes.CRT_002, excepcion.ErrorCode);
        Assert.Equal("El producto no se encuentra en el carrito.", excepcion.Message);
    }

    [Fact]
    public async Task UpdateItemAsync_ProductoQueYaNoExisteEnProducts_LanzaNotFoundConCRT002()
    {
        var discontinuado = Guid.NewGuid();
        await DadoElCarrito((discontinuado, 1));

        var excepcion = await Assert.ThrowsAsync<NotFoundException>(
            () => _service.UpdateItemAsync(Usuario, discontinuado, Cantidad(2)));

        Assert.Equal(ErrorCodes.CRT_002, excepcion.ErrorCode);
        Assert.Equal("Producto no encontrado.", excepcion.Message);
    }

    [Fact]
    public async Task UpdateItemAsync_StockInsuficiente_LanzaCRT003ComparandoContraLaCantidadNueva()
    {
        // En PUT la cantidad se reemplaza: se compara la cantidad nueva, no la suma.
        await DadoElCarrito((Auriculares, 2));

        var excepcion = await Assert.ThrowsAsync<BusinessRuleException>(
            () => _service.UpdateItemAsync(Usuario, Auriculares, Cantidad(4)));

        Assert.Equal(ErrorCodes.CRT_003, excepcion.ErrorCode);
        Assert.Equal("Stock insuficiente. Disponible: 3, solicitado: 4.", excepcion.Message);
    }

    // ---------- RemoveItemAsync ----------

    [Fact]
    public async Task RemoveItemAsync_ProductoEnElCarrito_LoQuitaYElCarritoSigueExistiendo()
    {
        await DadoElCarrito((Notebook, 1));

        await _service.RemoveItemAsync(Usuario, Notebook);

        var carrito = await _service.GetAsync(Usuario);
        Assert.Empty(carrito.Items);
        Assert.Equal(Ahora.UtcDateTime, carrito.FechaActualizacion);
    }

    [Fact]
    public async Task RemoveItemAsync_UsuarioSinCarrito_LanzaNotFoundConCRT001()
    {
        var excepcion = await Assert.ThrowsAsync<NotFoundException>(() => _service.RemoveItemAsync(Usuario, Notebook));

        Assert.Equal(ErrorCodes.CRT_001, excepcion.ErrorCode);
    }

    [Fact]
    public async Task RemoveItemAsync_ProductoQueNoEstaEnElCarrito_LanzaNotFoundConCRT002()
    {
        await DadoElCarrito((Notebook, 1));

        var excepcion = await Assert.ThrowsAsync<NotFoundException>(() => _service.RemoveItemAsync(Usuario, Auriculares));

        Assert.Equal(ErrorCodes.CRT_002, excepcion.ErrorCode);
    }

    // ---------- ClearAsync ----------

    [Fact]
    public async Task ClearAsync_CarritoExistente_LoElimina()
    {
        // D-27: vaciar elimina el carrito; después GET responde CRT-001.
        await DadoElCarrito((Notebook, 1), (Auriculares, 1));

        await _service.ClearAsync(Usuario);

        var excepcion = await Assert.ThrowsAsync<NotFoundException>(() => _service.GetAsync(Usuario));
        Assert.Equal(ErrorCodes.CRT_001, excepcion.ErrorCode);
    }

    [Fact]
    public async Task ClearAsync_UsuarioSinCarrito_LanzaNotFoundConCRT001()
    {
        var excepcion = await Assert.ThrowsAsync<NotFoundException>(() => _service.ClearAsync(Usuario));

        Assert.Equal(ErrorCodes.CRT_001, excepcion.ErrorCode);
    }

    // ---------- Helpers ----------

    private void DadoElProducto(Guid id, int stock) =>
        _productsClient.GetProductAsync(id, Arg.Any<CancellationToken>())
            .Returns(new ProductInfo { Id = id, Nombre = "Producto de prueba", Precio = 100m, Stock = stock });

    private Task DadoElCarrito(params (Guid ProductoId, int Cantidad)[] items)
    {
        var carrito = new ShoppingCart
        {
            UsuarioId = Usuario,
            FechaActualizacion = new DateTime(2024, 3, 10, 10, 45, 0, DateTimeKind.Utc)
        };
        foreach (var (productoId, cantidad) in items)
        {
            carrito.AddItem(productoId, cantidad);
        }
        return _repository.SaveAsync(carrito);
    }

    private static AddCartItemRequest Agregar(Guid productoId, int cantidad) =>
        new() { ProductoId = productoId, Cantidad = cantidad };

    private static UpdateCartItemRequest Cantidad(int cantidad) => new() { Cantidad = cantidad };
}
