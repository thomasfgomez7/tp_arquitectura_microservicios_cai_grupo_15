using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using Products.API.Clients;
using Products.API.DTOs;
using Products.API.Exceptions;
using Products.API.Models;
using Products.API.Repositories;
using Products.API.Services;

namespace Products.API.Tests.Unit.Services;

public class ProductServiceTests
{
    private static readonly DateTimeOffset Ahora = new(2026, 9, 27, 10, 30, 0, TimeSpan.Zero);

    private readonly IProductRepository _repository = Substitute.For<IProductRepository>();
    private readonly IOrdersClient _ordersClient = Substitute.For<IOrdersClient>();
    private readonly ProductService _service;

    public ProductServiceTests()
    {
        _service = new ProductService(_repository, _ordersClient, new FakeTimeProvider(Ahora));
    }

    // ---------- GetAllAsync ----------

    [Fact]
    public async Task GetAllAsync_SinFiltros_DevuelveTodosLosProductos()
    {
        DadosLosProductos(
            CrearProducto("Notebook Dell XPS 15", "Electrónica"),
            CrearProducto("Remera de algodón", "Indumentaria"));

        var resultado = await _service.GetAllAsync(categoria: null, nombre: null);

        Assert.Equal(2, resultado.Count);
    }

    [Fact]
    public async Task GetAllAsync_ConCategoria_DevuelveSoloEsaCategoriaSinDistinguirMayusculas()
    {
        DadosLosProductos(
            CrearProducto("Notebook Dell XPS 15", "Electrónica"),
            CrearProducto("Auriculares inalámbricos", "Electrónica"),
            CrearProducto("Remera de algodón", "Indumentaria"));

        var resultado = await _service.GetAllAsync(categoria: "ELECTRÓNICA", nombre: null);

        Assert.Equal(2, resultado.Count);
        Assert.All(resultado, p => Assert.Equal("Electrónica", p.Categoria));
    }

    [Fact]
    public async Task GetAllAsync_ConNombre_DevuelveCoincidenciasParcialesSinDistinguirMayusculas()
    {
        DadosLosProductos(
            CrearProducto("Notebook Dell XPS 15", "Electrónica"),
            CrearProducto("Notebook Lenovo ThinkPad", "Electrónica"),
            CrearProducto("Remera de algodón", "Indumentaria"));

        var resultado = await _service.GetAllAsync(categoria: null, nombre: "notebook");

        Assert.Equal(["Notebook Dell XPS 15", "Notebook Lenovo ThinkPad"], resultado.Select(p => p.Nombre));
    }

    [Fact]
    public async Task GetAllAsync_ConCategoriaYNombre_AplicaAmbosFiltros()
    {
        DadosLosProductos(
            CrearProducto("Mochila urbana", "Indumentaria"),
            CrearProducto("Mochila de trekking", "Deportes"),
            CrearProducto("Pelota de fútbol", "Deportes"));

        var resultado = await _service.GetAllAsync(categoria: "Deportes", nombre: "mochila");

        var producto = Assert.Single(resultado);
        Assert.Equal("Mochila de trekking", producto.Nombre);
    }

    // ---------- GetByIdAsync ----------

    [Fact]
    public async Task GetByIdAsync_ProductoExistente_DevuelveElProducto()
    {
        var existente = CrearProducto("Notebook Dell XPS 15", "Electrónica");
        DadoElProducto(existente);

        var resultado = await _service.GetByIdAsync(existente.Id);

        Assert.Equal(ProductResponse.FromEntity(existente), resultado);
    }

    [Fact]
    public async Task GetByIdAsync_ProductoInexistente_LanzaNotFoundConPRD001()
    {
        var excepcion = await Assert.ThrowsAsync<NotFoundException>(() => _service.GetByIdAsync(Guid.NewGuid()));

        Assert.Equal(ErrorCodes.PRD_001, excepcion.ErrorCode);
        Assert.Equal("Producto no encontrado.", excepcion.Message);
    }

    // ---------- CreateAsync ----------

    [Fact]
    public async Task CreateAsync_DatosValidos_AsignaIdYFechaCreacionYLoGuarda()
    {
        DadosLosProductos();
        var request = CrearRequest("Notebook Dell XPS 15", "Electrónica");

        var resultado = await _service.CreateAsync(request);

        Assert.NotEqual(Guid.Empty, resultado.Id);
        Assert.Equal(Ahora.UtcDateTime, resultado.FechaCreacion);
        Assert.Equal("Notebook Dell XPS 15", resultado.Nombre);
        Assert.Equal("Laptop 15 pulgadas, 32GB RAM", resultado.Descripcion);
        Assert.Equal(1500.00m, resultado.Precio);
        Assert.Equal(10, resultado.Stock);
        Assert.Equal("Electrónica", resultado.Categoria);
        await _repository.Received(1).AddAsync(
            Arg.Is<Product>(p => p.Id == resultado.Id && p.Nombre == request.Nombre),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_NombreDuplicadoEnMismaCategoria_LanzaBusinessRuleConPRD003()
    {
        DadosLosProductos(CrearProducto("Notebook Dell XPS 15", "Electrónica"));
        var request = CrearRequest("notebook dell xps 15", "ELECTRÓNICA");

        var excepcion = await Assert.ThrowsAsync<BusinessRuleException>(() => _service.CreateAsync(request));

        Assert.Equal(ErrorCodes.PRD_003, excepcion.ErrorCode);
        Assert.Equal(StatusCodes.Status409Conflict, excepcion.StatusCode);
        Assert.Equal("Ya existe un producto con ese nombre en la categoría 'ELECTRÓNICA'.", excepcion.Message);
        await _repository.DidNotReceive().AddAsync(Arg.Any<Product>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_MismoNombreEnOtraCategoria_LoCrea()
    {
        DadosLosProductos(CrearProducto("Mochila", "Indumentaria"));

        var resultado = await _service.CreateAsync(CrearRequest("Mochila", "Deportes"));

        Assert.Equal("Deportes", resultado.Categoria);
        await _repository.Received(1).AddAsync(Arg.Any<Product>(), Arg.Any<CancellationToken>());
    }

    // ---------- UpdateAsync ----------

    [Fact]
    public async Task UpdateAsync_ProductoExistente_ActualizaLosDatosYConservaIdYFechaCreacion()
    {
        var existente = CrearProducto("Notebook Dell XPS 15", "Electrónica");
        DadoElProducto(existente);
        var request = new UpdateProductRequest
        {
            Nombre = "Notebook Dell XPS 15",
            Descripcion = "Laptop 15 pulgadas, 64GB RAM",
            Precio = 1750.00m,
            Stock = 8,
            Categoria = "Electrónica"
        };

        var resultado = await _service.UpdateAsync(existente.Id, request);

        Assert.Equal(existente.Id, resultado.Id);
        Assert.Equal(existente.FechaCreacion, resultado.FechaCreacion);
        Assert.Equal("Laptop 15 pulgadas, 64GB RAM", resultado.Descripcion);
        Assert.Equal(1750.00m, resultado.Precio);
        Assert.Equal(8, resultado.Stock);
        await _repository.Received(1).UpdateAsync(
            Arg.Is<Product>(p => p.Id == existente.Id && p.Precio == 1750.00m && p.Stock == 8),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateAsync_ProductoInexistente_LanzaNotFoundConPRD001()
    {
        var request = new UpdateProductRequest { Nombre = "X", Precio = 1, Stock = 1, Categoria = "Otros" };

        var excepcion = await Assert.ThrowsAsync<NotFoundException>(() => _service.UpdateAsync(Guid.NewGuid(), request));

        Assert.Equal(ErrorCodes.PRD_001, excepcion.ErrorCode);
        await _repository.DidNotReceive().UpdateAsync(Arg.Any<Product>(), Arg.Any<CancellationToken>());
    }

    // ---------- DeleteAsync ----------

    [Fact]
    public async Task DeleteAsync_ProductoSinOrdenesActivas_LoElimina()
    {
        var existente = CrearProducto("Notebook Dell XPS 15", "Electrónica");
        DadoElProducto(existente);
        _ordersClient.HasActiveOrdersAsync(existente.Id, Arg.Any<CancellationToken>()).Returns(false);

        await _service.DeleteAsync(existente.Id);

        await _repository.Received(1).DeleteAsync(existente.Id, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteAsync_ProductoInexistente_LanzaNotFoundConPRD001SinConsultarOrdenes()
    {
        var excepcion = await Assert.ThrowsAsync<NotFoundException>(() => _service.DeleteAsync(Guid.NewGuid()));

        Assert.Equal(ErrorCodes.PRD_001, excepcion.ErrorCode);
        await _ordersClient.DidNotReceive().HasActiveOrdersAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        await _repository.DidNotReceive().DeleteAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteAsync_ProductoConOrdenesActivas_LanzaBusinessRuleConPRD004YNoLoElimina()
    {
        var existente = CrearProducto("Notebook Dell XPS 15", "Electrónica");
        DadoElProducto(existente);
        _ordersClient.HasActiveOrdersAsync(existente.Id, Arg.Any<CancellationToken>()).Returns(true);

        var excepcion = await Assert.ThrowsAsync<BusinessRuleException>(() => _service.DeleteAsync(existente.Id));

        Assert.Equal(ErrorCodes.PRD_004, excepcion.ErrorCode);
        Assert.Equal(StatusCodes.Status409Conflict, excepcion.StatusCode);
        Assert.Equal("El producto tiene órdenes activas y no puede eliminarse.", excepcion.Message);
        await _repository.DidNotReceive().DeleteAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    // ---------- Helpers ----------

    private void DadosLosProductos(params Product[] productos) =>
        _repository.GetAllAsync(Arg.Any<CancellationToken>()).Returns(productos);

    private void DadoElProducto(Product producto) =>
        _repository.GetByIdAsync(producto.Id, Arg.Any<CancellationToken>()).Returns(producto);

    private static Product CrearProducto(string nombre, string categoria) => new()
    {
        Id = Guid.NewGuid(),
        Nombre = nombre,
        Descripcion = "Descripción de prueba",
        Precio = 100.00m,
        Stock = 5,
        Categoria = categoria,
        FechaCreacion = new DateTime(2024, 1, 15, 10, 30, 0, DateTimeKind.Utc)
    };

    private static CreateProductRequest CrearRequest(string nombre, string categoria) => new()
    {
        Nombre = nombre,
        Descripcion = "Laptop 15 pulgadas, 32GB RAM",
        Precio = 1500.00m,
        Stock = 10,
        Categoria = categoria
    };
}
