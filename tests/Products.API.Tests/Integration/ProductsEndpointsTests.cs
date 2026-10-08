using System.Net;
using System.Net.Http.Json;
using System.Text;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Products.API.Clients;
using Products.API.DTOs;
using Products.API.Exceptions;

namespace Products.API.Tests.Integration;

public class ProductsEndpointsTests(ProductsApiFactory factory) : IClassFixture<ProductsApiFactory>
{
    // Producto de los datos semilla (ProductSeedData); ningún test lo modifica.
    private const string NotebookId = "3fa85f64-5717-4562-b3fc-2c963f66afa6";

    private readonly HttpClient _client = factory.CreateClient();

    // ---------- GET /api/products ----------

    [Fact]
    public async Task GetAll_SinFiltros_Devuelve200ConLosProductos()
    {
        var response = await _client.GetAsync("/api/products");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var productos = await response.Content.ReadFromJsonAsync<List<ProductResponse>>();
        Assert.Contains(productos!, p => p.Id == Guid.Parse(NotebookId));
    }

    [Fact]
    public async Task GetAll_ConCategoriaYNombre_Devuelve200Filtrado()
    {
        var response = await _client.GetAsync("/api/products?categoria=electrónica&nombre=notebook");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var productos = await response.Content.ReadFromJsonAsync<List<ProductResponse>>();
        var producto = Assert.Single(productos!);
        Assert.Equal("Notebook Dell XPS 15", producto.Nombre);
    }

    // ---------- GET /api/products/{id} ----------

    [Fact]
    public async Task GetById_ProductoExistente_Devuelve200ConElProducto()
    {
        var response = await _client.GetAsync($"/api/products/{NotebookId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var producto = await response.Content.ReadFromJsonAsync<ProductResponse>();
        Assert.Equal("Notebook Dell XPS 15", producto!.Nombre);
        Assert.Equal(new DateTime(2024, 1, 15, 10, 30, 0, DateTimeKind.Utc), producto.FechaCreacion);
    }

    [Fact]
    public async Task GetById_ProductoInexistente_Devuelve404ConPRD001()
    {
        var id = Guid.NewGuid();

        var response = await _client.GetAsync($"/api/products/{id}");

        var body = await ErrorContractAssert.IsErrorAsync(response, HttpStatusCode.NotFound, ErrorCodes.PRD_001,
            instance: $"/api/products/{id}", errorMessage: "Producto no encontrado.");
        Assert.Equal("Not Found", body.GetProperty("title").GetString());
        Assert.Equal("https://tools.ietf.org/html/rfc7231#section-6.5.4", body.GetProperty("type").GetString());
        Assert.Equal("El recurso solicitado no fue encontrado.", body.GetProperty("detail").GetString());
    }

    [Fact]
    public async Task GetById_IdQueNoEsGuid_Devuelve404ConPRD001()
    {
        // Ejemplo literal del enunciado: GET /api/products/99 → 404 (PRD-001)
        var response = await _client.GetAsync("/api/products/99");

        await ErrorContractAssert.IsErrorAsync(response, HttpStatusCode.NotFound, ErrorCodes.PRD_001,
            instance: "/api/products/99", errorMessage: "Producto no encontrado.");
    }

    // ---------- POST /api/products ----------

    [Fact]
    public async Task Create_DatosValidos_Devuelve201ConLocationYElProducto()
    {
        var request = NuevoProducto();

        var response = await _client.PostAsJsonAsync("/api/products", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var creado = await response.Content.ReadFromJsonAsync<ProductResponse>();
        Assert.Equal(request.Nombre, creado!.Nombre);
        Assert.NotEqual(Guid.Empty, creado.Id);
        Assert.EndsWith($"/api/products/{creado.Id}", response.Headers.Location!.ToString());
    }

    [Fact]
    public async Task Create_NombreDuplicadoEnCategoria_Devuelve409ConPRD003()
    {
        var request = new CreateProductRequest
        {
            Nombre = "notebook dell xps 15",
            Precio = 1500m,
            Stock = 1,
            Categoria = "Electrónica"
        };

        var response = await _client.PostAsJsonAsync("/api/products", request);

        var body = await ErrorContractAssert.IsErrorAsync(response, HttpStatusCode.Conflict, ErrorCodes.PRD_003,
            instance: "/api/products",
            errorMessage: "Ya existe un producto con ese nombre en la categoría 'Electrónica'.");
        Assert.Equal("Conflict", body.GetProperty("title").GetString());
        Assert.Equal("Ya existe un recurso con esos datos.", body.GetProperty("detail").GetString());
    }

    [Fact]
    public async Task Create_DatosInvalidos_Devuelve400ConPRD002YTodosLosProblemas()
    {
        var request = new { nombre = "", precio = 0, categoria = "Otros" };

        var response = await _client.PostAsJsonAsync("/api/products", request);

        var body = await ErrorContractAssert.IsErrorAsync(response, HttpStatusCode.BadRequest, ErrorCodes.PRD_002,
            instance: "/api/products");
        var mensaje = body.GetProperty("errorMessage").GetString()!;
        var problemas = mensaje.TrimEnd('.').Split("; ");
        Assert.Equal(3, problemas.Length);
        Assert.Contains("El nombre es obligatorio", problemas);
        Assert.Contains("El precio debe ser mayor a 0", problemas);
        Assert.Contains("El stock es obligatorio", problemas);
        Assert.DoesNotContain(".;", mensaje);
        Assert.EndsWith(".", mensaje);
    }

    [Fact]
    public async Task Create_JsonMalFormado_Devuelve400ConPRD002()
    {
        var contenido = new StringContent("{ \"nombre\": ", Encoding.UTF8, "application/json");

        var response = await _client.PostAsync("/api/products", contenido);

        await ErrorContractAssert.IsErrorAsync(response, HttpStatusCode.BadRequest, ErrorCodes.PRD_002,
            instance: "/api/products", errorMessage: "El cuerpo de la solicitud no es un JSON válido.");
    }

    // ---------- PUT /api/products/{id} ----------

    [Fact]
    public async Task Update_ProductoExistente_Devuelve200ConLosDatosNuevos()
    {
        var creado = await CrearProductoAsync();
        var request = new UpdateProductRequest
        {
            Nombre = creado.Nombre,
            Descripcion = "Descripción actualizada",
            Precio = 99.99m,
            Stock = 3,
            Categoria = creado.Categoria
        };

        var response = await _client.PutAsJsonAsync($"/api/products/{creado.Id}", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var actualizado = await response.Content.ReadFromJsonAsync<ProductResponse>();
        Assert.Equal(99.99m, actualizado!.Precio);
        Assert.Equal(creado.FechaCreacion, actualizado.FechaCreacion);
    }

    [Fact]
    public async Task Update_ProductoInexistente_Devuelve404ConPRD001()
    {
        var id = Guid.NewGuid();

        var response = await _client.PutAsJsonAsync($"/api/products/{id}", NuevoProducto());

        await ErrorContractAssert.IsErrorAsync(response, HttpStatusCode.NotFound, ErrorCodes.PRD_001,
            instance: $"/api/products/{id}");
    }

    [Fact]
    public async Task Update_DatosInvalidos_Devuelve400ConPRD002()
    {
        var request = new { nombre = new string('x', 101), precio = 10, stock = -1, categoria = "Otros" };

        var response = await _client.PutAsJsonAsync($"/api/products/{NotebookId}", request);

        var body = await ErrorContractAssert.IsErrorAsync(response, HttpStatusCode.BadRequest, ErrorCodes.PRD_002,
            instance: $"/api/products/{NotebookId}");
        var mensaje = body.GetProperty("errorMessage").GetString()!;
        Assert.Contains("El nombre no puede superar los 100 caracteres", mensaje);
        Assert.Contains("El stock debe ser mayor o igual a 0", mensaje);
    }

    // ---------- DELETE /api/products/{id} ----------

    [Fact]
    public async Task Delete_ProductoExistente_Devuelve204YLuegoNoExiste()
    {
        var creado = await CrearProductoAsync();

        var response = await _client.DeleteAsync($"/api/products/{creado.Id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(0, response.Content.Headers.ContentLength ?? 0);
        var get = await _client.GetAsync($"/api/products/{creado.Id}");
        Assert.Equal(HttpStatusCode.NotFound, get.StatusCode);
    }

    [Fact]
    public async Task Delete_ProductoInexistente_Devuelve404ConPRD001()
    {
        var id = Guid.NewGuid();

        var response = await _client.DeleteAsync($"/api/products/{id}");

        await ErrorContractAssert.IsErrorAsync(response, HttpStatusCode.NotFound, ErrorCodes.PRD_001,
            instance: $"/api/products/{id}");
    }

    [Fact]
    public async Task Delete_ProductoConOrdenesActivas_Devuelve409ConPRD004()
    {
        var ordersClient = Substitute.For<IOrdersClient>();
        ordersClient.HasActiveOrdersAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(true);
        var client = factory
            .WithWebHostBuilder(b => b.ConfigureTestServices(s => s.AddSingleton(ordersClient)))
            .CreateClient();

        var response = await client.DeleteAsync($"/api/products/{NotebookId}");

        var body = await ErrorContractAssert.IsErrorAsync(response, HttpStatusCode.Conflict, ErrorCodes.PRD_004,
            instance: $"/api/products/{NotebookId}",
            errorMessage: "El producto tiene órdenes activas y no puede eliminarse.");
        Assert.Equal("No se puede eliminar el recurso.", body.GetProperty("detail").GetString());
    }

    // ---------- Helpers ----------

    private static CreateProductRequest NuevoProducto() => new()
    {
        Nombre = $"Producto de prueba {Guid.NewGuid():N}",
        Descripcion = "Creado por un test de integración",
        Precio = 10.50m,
        Stock = 5,
        Categoria = "Otros"
    };

    private async Task<ProductResponse> CrearProductoAsync()
    {
        var response = await _client.PostAsJsonAsync("/api/products", NuevoProducto());
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<ProductResponse>())!;
    }
}
