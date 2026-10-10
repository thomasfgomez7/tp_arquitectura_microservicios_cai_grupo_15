using System.Net;
using System.Text;
using Orders.API.Clients;

namespace Orders.API.Tests.Unit.Clients;

public class ProductsClientTests
{
    private static readonly Guid ProductoId = Guid.Parse("3fa85f64-5717-4562-b3fc-2c963f66afa6");

    [Fact]
    public async Task GetProductAsync_ProductsResponde200_DevuelveElProducto()
    {
        var handler = new FakeHttpHandler(HttpStatusCode.OK, """
            {
              "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
              "nombre": "Notebook Dell XPS 15",
              "descripcion": "Laptop 15 pulgadas, 32GB RAM",
              "precio": 1500.00,
              "stock": 10,
              "categoria": "Electrónica",
              "fechaCreacion": "2024-01-15T10:30:00Z"
            }
            """);

        var producto = await CrearCliente(handler).GetProductAsync(ProductoId);

        Assert.NotNull(producto);
        Assert.Equal(ProductoId, producto.Id);
        Assert.Equal("Notebook Dell XPS 15", producto.Nombre);
        Assert.Equal(1500.00m, producto.Precio);
        Assert.Equal(10, producto.Stock);
    }

    [Fact]
    public async Task GetProductAsync_LlamaAlEndpointDelContrato()
    {
        var handler = new FakeHttpHandler(HttpStatusCode.NotFound, "");

        await CrearCliente(handler).GetProductAsync(ProductoId);

        Assert.Equal(HttpMethod.Get, handler.UltimoRequest!.Method);
        Assert.Equal($"http://products.test/api/products/{ProductoId}", handler.UltimoRequest.RequestUri!.ToString());
    }

    [Fact]
    public async Task GetProductAsync_ProductsResponde404_DevuelveNull()
    {
        // El body del 404 es el JSON de error de Products: no se intenta leer como producto.
        var handler = new FakeHttpHandler(HttpStatusCode.NotFound, """{ "errorCode": "PRD-001" }""");

        var producto = await CrearCliente(handler).GetProductAsync(ProductoId);

        Assert.Null(producto);
    }

    [Theory]
    [InlineData(HttpStatusCode.InternalServerError)]
    [InlineData(HttpStatusCode.ServiceUnavailable)]
    [InlineData(HttpStatusCode.BadRequest)]
    public async Task GetProductAsync_ProductsRespondeOtroError_LanzaHttpRequestException(HttpStatusCode status)
    {
        // D-36: la excepción llega al GlobalExceptionHandler y termina en un 500 con ORD-007.
        var handler = new FakeHttpHandler(status, """{ "errorCode": "PRD-005" }""");

        await Assert.ThrowsAsync<HttpRequestException>(() => CrearCliente(handler).GetProductAsync(ProductoId));
    }

    [Fact]
    public async Task GetProductAsync_ProductsNoResponde_PropagaLaExcepcion()
    {
        var handler = new FakeHttpHandler(new HttpRequestException("Connection refused"));

        await Assert.ThrowsAsync<HttpRequestException>(() => CrearCliente(handler).GetProductAsync(ProductoId));
    }

    private static ProductsClient CrearCliente(FakeHttpHandler handler) =>
        new(new HttpClient(handler) { BaseAddress = new Uri("http://products.test/") });

    /// <summary>
    /// Reemplaza la red: devuelve una respuesta fija (o lanza una excepción) y guarda el request recibido.
    /// </summary>
    private sealed class FakeHttpHandler : HttpMessageHandler
    {
        private readonly HttpStatusCode _status;
        private readonly string _body = "";
        private readonly Exception? _error;

        public FakeHttpHandler(HttpStatusCode status, string body) => (_status, _body) = (status, body);

        public FakeHttpHandler(Exception error) => _error = error;

        public HttpRequestMessage? UltimoRequest { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            UltimoRequest = request;
            if (_error is not null)
            {
                throw _error;
            }

            return Task.FromResult(new HttpResponseMessage(_status)
            {
                Content = new StringContent(_body, Encoding.UTF8, "application/json")
            });
        }
    }
}
