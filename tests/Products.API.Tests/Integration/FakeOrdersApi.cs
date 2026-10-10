using System.Collections.Concurrent;
using System.Net;
using System.Text;
using System.Text.Json;

namespace Products.API.Tests.Integration;

/// <summary>
/// Orders.API falso: responde GET /health/live y GET /api/orders?productoId= como el servicio real.
/// Reemplaza la red del cliente HTTP de Orders, así los tests usan el OrdersClient real
/// (con su DelegatingHandler y su parseo) sin levantar Orders.API.
/// </summary>
public class FakeOrdersApi
{
    private readonly ConcurrentBag<(Guid ProductoId, string Estado)> _ordenes = [];

    /// <summary>Si es true, toda llamada falla como si Orders.API estuviera caído.</summary>
    public bool Caido { get; set; }

    /// <summary>Correlation ID recibido en cada llamada, en orden de llegada (null si no vino).</summary>
    public ConcurrentQueue<string?> CorrelationIdsRecibidos { get; } = new();

    public void AgregarOrden(Guid productoId, string estado) => _ordenes.Add((productoId, estado));

    /// <summary>Un handler nuevo por cliente: IHttpClientFactory descarta los handlers viejos.</summary>
    public HttpMessageHandler CrearHandler() => new Handler(this);

    private sealed class Handler(FakeOrdersApi api) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            api.CorrelationIdsRecibidos.Enqueue(
                request.Headers.TryGetValues("X-Correlation-Id", out var values) ? values.Single() : null);

            if (api.Caido)
            {
                throw new HttpRequestException("Connection refused (orders.test)");
            }

            return Task.FromResult(request.RequestUri!.AbsolutePath switch
            {
                "/health/live" => Json(HttpStatusCode.OK, new { status = "Healthy" }),
                "/api/orders" => Json(HttpStatusCode.OK, Ordenes(request.RequestUri)),
                _ => Json(HttpStatusCode.NotFound, new { errorCode = "ORD-001" })
            });
        }

        private object[] Ordenes(Uri uri)
        {
            var filtro = System.Web.HttpUtility.ParseQueryString(uri.Query)["productoId"];

            return api._ordenes
                .Where(orden => filtro is null || orden.ProductoId.ToString() == filtro)
                .Select(orden => (object)new
                {
                    id = Guid.NewGuid(),
                    usuarioId = Guid.Parse("a1b2c3d4-0000-0000-0000-111122223333"),
                    items = new[] { new { productoId = orden.ProductoId, cantidad = 1, precioUnitario = 1500.00m } },
                    total = 1500.00m,
                    estado = orden.Estado,
                    fechaCreacion = new DateTime(2024, 3, 10, 11, 0, 0, DateTimeKind.Utc)
                })
                .ToArray();
        }

        private static HttpResponseMessage Json(HttpStatusCode status, object body) => new(status)
        {
            Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json")
        };
    }
}
