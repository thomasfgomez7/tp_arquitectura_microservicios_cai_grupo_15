using System.Net;
using Products.API.Clients;

namespace Products.API.Tests.Unit.Clients;

/// <summary>
/// OrdersClient contra el contrato de GET /api/orders?productoId= (D-07, sección 4.4 del plan).
/// </summary>
public class OrdersClientTests
{
    private static readonly Guid ProductoId = Guid.Parse("3fa85f64-5717-4562-b3fc-2c963f66afa6");

    [Fact]
    public async Task HasActiveOrdersAsync_LlamaAlEndpointDelContratoConElFiltroDeProducto()
    {
        var handler = new FakeHttpHandler(HttpStatusCode.OK, "[]");

        await CrearCliente(handler).HasActiveOrdersAsync(ProductoId);

        Assert.Equal(HttpMethod.Get, handler.UltimoRequest!.Method);
        Assert.Equal($"http://orders.test/api/orders?productoId={ProductoId}", handler.UltimoRequest.RequestUri!.ToString());
    }

    [Fact]
    public async Task HasActiveOrdersAsync_SinOrdenes_DevuelveFalse()
    {
        // D-37: un producto sin órdenes responde 200 con una lista vacía, no 404.
        var handler = new FakeHttpHandler(HttpStatusCode.OK, "[]");

        Assert.False(await CrearCliente(handler).HasActiveOrdersAsync(ProductoId));
    }

    [Theory]
    [InlineData("Pendiente")]
    [InlineData("Confirmada")]
    public async Task HasActiveOrdersAsync_ConUnaOrdenActiva_DevuelveTrue(string estado)
    {
        var handler = new FakeHttpHandler(HttpStatusCode.OK, Ordenes("Entregada", estado));

        Assert.True(await CrearCliente(handler).HasActiveOrdersAsync(ProductoId));
    }

    [Fact]
    public async Task HasActiveOrdersAsync_SoloOrdenesEnviadasEntregadasOCanceladas_DevuelveFalse()
    {
        // Sección 4.1 del enunciado: PRD-004 solo aplica a órdenes Pendiente o Confirmada.
        var handler = new FakeHttpHandler(HttpStatusCode.OK, Ordenes("Enviada", "Entregada", "Cancelada"));

        Assert.False(await CrearCliente(handler).HasActiveOrdersAsync(ProductoId));
    }

    [Theory]
    [InlineData(HttpStatusCode.InternalServerError)]
    [InlineData(HttpStatusCode.ServiceUnavailable)]
    [InlineData(HttpStatusCode.NotFound)]
    public async Task HasActiveOrdersAsync_OrdersRespondeUnError_LanzaHttpRequestException(HttpStatusCode status)
    {
        // D-39: sin saber si hay órdenes activas no se borra; termina en un 500 con PRD-005.
        var handler = new FakeHttpHandler(status, """{ "errorCode": "ORD-007" }""");

        await Assert.ThrowsAsync<HttpRequestException>(() => CrearCliente(handler).HasActiveOrdersAsync(ProductoId));
    }

    [Fact]
    public async Task HasActiveOrdersAsync_OrdersNoResponde_PropagaLaExcepcion()
    {
        var handler = new FakeHttpHandler(new HttpRequestException("Connection refused"));

        await Assert.ThrowsAsync<HttpRequestException>(() => CrearCliente(handler).HasActiveOrdersAsync(ProductoId));
    }

    private static OrdersClient CrearCliente(FakeHttpHandler handler) =>
        new(new HttpClient(handler) { BaseAddress = new Uri("http://orders.test/") });

    // Órdenes con el formato de GET /api/orders/{id}, una por estado.
    private static string Ordenes(params string[] estados) =>
        "[" + string.Join(",", estados.Select(estado => $$"""
            {
              "id": "{{Guid.NewGuid()}}",
              "usuarioId": "a1b2c3d4-0000-0000-0000-111122223333",
              "items": [ { "productoId": "{{ProductoId}}", "cantidad": 1, "precioUnitario": 1500.00 } ],
              "total": 1500.00,
              "estado": "{{estado}}",
              "fechaCreacion": "2024-03-10T11:00:00Z"
            }
            """)) + "]";
}
