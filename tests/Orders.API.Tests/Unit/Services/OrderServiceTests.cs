using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using Orders.API.Clients;
using Orders.API.DTOs;
using Orders.API.Exceptions;
using Orders.API.Models;
using Orders.API.Repositories;
using Orders.API.Services;

namespace Orders.API.Tests.Unit.Services;

/// <summary>
/// El repositorio es el real en memoria (funciona como un "fake": guarda de verdad, sin red ni disco),
/// porque los casos encadenan operaciones sobre la misma orden. Users.API y Products.API se reemplazan por dobles.
/// </summary>
public class OrderServiceTests
{
    private static readonly DateTimeOffset Ahora = new(2026, 10, 1, 9, 0, 0, TimeSpan.Zero);
    private static readonly Guid Usuario = Guid.NewGuid();
    private static readonly Guid Notebook = Guid.NewGuid();
    private static readonly Guid Auriculares = Guid.NewGuid();

    private readonly InMemoryOrderRepository _repository = new();
    private readonly IUsersClient _usersClient = Substitute.For<IUsersClient>();
    private readonly IProductsClient _productsClient = Substitute.For<IProductsClient>();
    private readonly FakeTimeProvider _timeProvider = new(Ahora);
    private readonly OrderService _service;

    public OrderServiceTests()
    {
        _service = new OrderService(_repository, _usersClient, _productsClient, _timeProvider);
        DadoElUsuario(Usuario, activo: true);
        DadoElProducto(Notebook, "Notebook Dell XPS 15", precio: 1500m, stock: 10);
        DadoElProducto(Auriculares, "Auriculares inalámbricos", precio: 200m, stock: 5);
    }

    // ---------- GetAllAsync ----------

    [Fact]
    public async Task GetAllAsync_SinOrdenes_DevuelveListaVacia()
    {
        Assert.Empty(await _service.GetAllAsync());
    }

    [Fact]
    public async Task GetAllAsync_FiltroPorProducto_DevuelveLasOrdenesConEseProducto()
    {
        await DadaLaOrden(OrderStatus.Pendiente, Notebook);
        var conAuriculares = await DadaLaOrden(OrderStatus.Confirmada, Auriculares);

        var ordenes = await _service.GetAllAsync(productoId: Auriculares);

        var orden = Assert.Single(ordenes);
        Assert.Equal(conAuriculares.Id, orden.Id);
        Assert.Equal("Confirmada", orden.Estado);
    }

    // ---------- GetByIdAsync ----------

    [Fact]
    public async Task GetByIdAsync_OrdenExistente_DevuelveLaOrden()
    {
        var existente = await DadaLaOrden(OrderStatus.Pendiente, Notebook);

        var orden = await _service.GetByIdAsync(existente.Id);

        Assert.Equal(existente.Id, orden.Id);
        Assert.Equal(Usuario, orden.UsuarioId);
        Assert.Equal("Pendiente", orden.Estado);
        Assert.Equal(Notebook, Assert.Single(orden.Items).ProductoId);
    }

    [Fact]
    public async Task GetByIdAsync_OrdenInexistente_LanzaNotFoundConORD001()
    {
        var excepcion = await Assert.ThrowsAsync<NotFoundException>(() => _service.GetByIdAsync(Guid.NewGuid()));

        Assert.Equal(ErrorCodes.ORD_001, excepcion.ErrorCode);
        Assert.Equal("Orden no encontrada.", excepcion.Message);
    }

    // ---------- CreateAsync ----------

    [Fact]
    public async Task CreateAsync_DatosValidos_CreaLaOrdenPendienteConElPrecioDelProductoYElTotal()
    {
        var orden = await _service.CreateAsync(Crear(Usuario, (Notebook, 2), (Auriculares, 1)));

        Assert.Equal(Usuario, orden.UsuarioId);
        Assert.Equal("Pendiente", orden.Estado);
        Assert.Equal(Ahora.UtcDateTime, orden.FechaCreacion);
        Assert.Collection(
            orden.Items,
            item => Assert.Equal((Notebook, 2, 1500m), (item.ProductoId, item.Cantidad, item.PrecioUnitario)),
            item => Assert.Equal((Auriculares, 1, 200m), (item.ProductoId, item.Cantidad, item.PrecioUnitario)));
        Assert.Equal(3200m, orden.Total);
        Assert.NotNull(await _repository.GetByIdAsync(orden.Id));
    }

    [Fact]
    public async Task CreateAsync_ProductoRepetido_UneLosItemsYSumaLasCantidades()
    {
        var orden = await _service.CreateAsync(Crear(Usuario, (Notebook, 1), (Notebook, 2)));

        var item = Assert.Single(orden.Items);
        Assert.Equal(3, item.Cantidad);
        Assert.Equal(4500m, orden.Total);
    }

    [Fact]
    public async Task CreateAsync_CantidadIgualAlStock_CreaLaOrden()
    {
        var orden = await _service.CreateAsync(Crear(Usuario, (Auriculares, 5)));

        Assert.Equal(5, Assert.Single(orden.Items).Cantidad);
    }

    [Fact]
    public async Task CreateAsync_UsuarioBloqueado_CreaLaOrden()
    {
        var bloqueado = Guid.NewGuid();
        DadoElUsuario(bloqueado, activo: false);

        var orden = await _service.CreateAsync(Crear(bloqueado, (Notebook, 1)));

        Assert.Equal(bloqueado, orden.UsuarioId);
    }

    [Fact]
    public async Task CreateAsync_SinUsuario_LanzaValidationConORD002()
    {
        var request = new CreateOrderRequest { UsuarioId = null, Items = [Item(Notebook, 1)] };

        var excepcion = await Assert.ThrowsAsync<ValidationException>(() => _service.CreateAsync(request));

        Assert.Equal(ErrorCodes.ORD_002, excepcion.ErrorCode);
        Assert.Equal("El usuario es obligatorio.", excepcion.Message);
    }

    [Fact]
    public async Task CreateAsync_SinItems_LanzaValidationConORD002()
    {
        var excepcion = await Assert.ThrowsAsync<ValidationException>(() => _service.CreateAsync(Crear(Usuario)));

        Assert.Equal(ErrorCodes.ORD_002, excepcion.ErrorCode);
        Assert.Equal("La orden debe tener al menos un item.", excepcion.Message);
    }

    [Theory]
    [InlineData(false, 1)]     // sin producto
    [InlineData(true, null)]   // sin cantidad
    [InlineData(true, 0)]
    [InlineData(true, -1)]
    public async Task CreateAsync_ItemInvalido_LanzaValidationConORD002(bool conProducto, int? cantidad)
    {
        var item = new CreateOrderItemRequest { ProductoId = conProducto ? Notebook : null, Cantidad = cantidad };
        var request = new CreateOrderRequest { UsuarioId = Usuario, Items = [item] };

        var excepcion = await Assert.ThrowsAsync<ValidationException>(() => _service.CreateAsync(request));

        Assert.Equal(ErrorCodes.ORD_002, excepcion.ErrorCode);
        Assert.Equal("Los items de la orden son inválidos.", excepcion.Message);
    }

    [Fact]
    public async Task CreateAsync_UsuarioInexistente_LanzaNotFoundConORD003()
    {
        var excepcion = await Assert.ThrowsAsync<NotFoundException>(
            () => _service.CreateAsync(Crear(Guid.NewGuid(), (Notebook, 1))));

        Assert.Equal(ErrorCodes.ORD_003, excepcion.ErrorCode);
        Assert.Equal("Usuario no encontrado al crear la orden.", excepcion.Message);
        await _productsClient.DidNotReceive().GetProductAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_ProductoInexistente_LanzaNotFoundConORD004YNoGuardaLaOrden()
    {
        var excepcion = await Assert.ThrowsAsync<NotFoundException>(
            () => _service.CreateAsync(Crear(Usuario, (Notebook, 1), (Guid.NewGuid(), 1))));

        Assert.Equal(ErrorCodes.ORD_004, excepcion.ErrorCode);
        Assert.Equal("Producto no encontrado al crear la orden.", excepcion.Message);
        Assert.Empty(await _repository.GetAllAsync());
    }

    [Fact]
    public async Task CreateAsync_StockInsuficiente_LanzaBusinessRuleConORD005()
    {
        var excepcion = await Assert.ThrowsAsync<BusinessRuleException>(
            () => _service.CreateAsync(Crear(Usuario, (Notebook, 11))));

        Assert.Equal(ErrorCodes.ORD_005, excepcion.ErrorCode);
        Assert.Equal(StatusCodes.Status422UnprocessableEntity, excepcion.StatusCode);
        Assert.Equal(
            "Stock insuficiente para 'Notebook Dell XPS 15'. Disponible: 10, solicitado: 11.",
            excepcion.Message);
        Assert.Empty(await _repository.GetAllAsync());
    }

    [Fact]
    public async Task CreateAsync_ProductoRepetidoQueSuperaElStock_LanzaBusinessRuleConORD005()
    {
        var excepcion = await Assert.ThrowsAsync<BusinessRuleException>(
            () => _service.CreateAsync(Crear(Usuario, (Auriculares, 3), (Auriculares, 3))));

        Assert.Equal(ErrorCodes.ORD_005, excepcion.ErrorCode);
        Assert.Equal(
            "Stock insuficiente para 'Auriculares inalámbricos'. Disponible: 5, solicitado: 6.",
            excepcion.Message);
    }

    // ---------- UpdateStatusAsync ----------

    [Fact]
    public async Task UpdateStatusAsync_TransicionValida_CambiaElEstadoYLaFecha()
    {
        var existente = await DadaLaOrden(OrderStatus.Pendiente, Notebook);
        _timeProvider.Advance(TimeSpan.FromHours(1));

        var respuesta = await _service.UpdateStatusAsync(existente.Id, Estado("Confirmada"));

        Assert.Equal(existente.Id, respuesta.Id);
        Assert.Equal("Confirmada", respuesta.Estado);
        Assert.Equal(Ahora.UtcDateTime.AddHours(1), respuesta.FechaActualizacion);
        Assert.Equal(OrderStatus.Confirmada, (await _repository.GetByIdAsync(existente.Id))!.Estado);
    }

    [Fact]
    public async Task UpdateStatusAsync_TransicionInvalida_LanzaBusinessRuleConORD006YNoCambiaElEstado()
    {
        var existente = await DadaLaOrden(OrderStatus.Entregada, Notebook);

        var excepcion = await Assert.ThrowsAsync<BusinessRuleException>(
            () => _service.UpdateStatusAsync(existente.Id, Estado("Pendiente")));

        Assert.Equal(ErrorCodes.ORD_006, excepcion.ErrorCode);
        Assert.Equal(StatusCodes.Status409Conflict, excepcion.StatusCode);
        Assert.Equal("Una orden en estado 'Entregada' no puede pasar a 'Pendiente'.", excepcion.Message);
        Assert.Equal(OrderStatus.Entregada, (await _repository.GetByIdAsync(existente.Id))!.Estado);
    }

    [Theory]
    [InlineData("Pagada")]
    [InlineData("pendiente")]
    [InlineData("")]
    public async Task UpdateStatusAsync_EstadoDesconocido_LanzaValidationConORD002(string estado)
    {
        var existente = await DadaLaOrden(OrderStatus.Pendiente, Notebook);

        var excepcion = await Assert.ThrowsAsync<ValidationException>(
            () => _service.UpdateStatusAsync(existente.Id, Estado(estado)));

        Assert.Equal(ErrorCodes.ORD_002, excepcion.ErrorCode);
        Assert.Equal("El estado debe ser Pendiente, Confirmada, Enviada, Entregada o Cancelada.", excepcion.Message);
    }

    [Fact]
    public async Task UpdateStatusAsync_OrdenInexistente_LanzaNotFoundConORD001()
    {
        var excepcion = await Assert.ThrowsAsync<NotFoundException>(
            () => _service.UpdateStatusAsync(Guid.NewGuid(), Estado("Confirmada")));

        Assert.Equal(ErrorCodes.ORD_001, excepcion.ErrorCode);
        Assert.Equal("Orden no encontrada.", excepcion.Message);
    }

    private void DadoElUsuario(Guid id, bool activo) =>
        _usersClient.GetUserAsync(id, Arg.Any<CancellationToken>())
            .Returns(new UserInfo
            {
                Id = id,
                Nombre = "María",
                Apellido = "González",
                Email = "maria@email.com",
                Activo = activo
            });

    private void DadoElProducto(Guid id, string nombre, decimal precio, int stock) =>
        _productsClient.GetProductAsync(id, Arg.Any<CancellationToken>())
            .Returns(new ProductInfo { Id = id, Nombre = nombre, Precio = precio, Stock = stock });

    private async Task<Order> DadaLaOrden(OrderStatus estado, Guid productoId)
    {
        var orden = new Order
        {
            Id = Guid.NewGuid(),
            UsuarioId = Usuario,
            Items = [new OrderItem { ProductoId = productoId, Cantidad = 1, PrecioUnitario = 1500m }],
            Total = 1500m,
            Estado = estado,
            FechaCreacion = new DateTime(2024, 3, 10, 11, 0, 0, DateTimeKind.Utc),
            FechaActualizacion = new DateTime(2024, 3, 10, 11, 0, 0, DateTimeKind.Utc)
        };
        await _repository.AddAsync(orden);
        return orden;
    }

    private static CreateOrderItemRequest Item(Guid productoId, int cantidad) =>
        new() { ProductoId = productoId, Cantidad = cantidad };

    private static CreateOrderRequest Crear(Guid usuarioId, params (Guid ProductoId, int Cantidad)[] items) =>
        new() { UsuarioId = usuarioId, Items = items.Select(item => Item(item.ProductoId, item.Cantidad)).ToList() };

    private static UpdateOrderStatusRequest Estado(string estado) => new() { Estado = estado };
}
