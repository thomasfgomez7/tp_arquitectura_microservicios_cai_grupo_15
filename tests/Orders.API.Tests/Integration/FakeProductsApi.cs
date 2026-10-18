using System.Collections.Concurrent;
using System.Net;
using System.Text;
using System.Text.Json;

namespace Orders.API.Tests.Integration;

/// <summary>
/// Products.API falso: responde GET /health/live y GET /api/products/{id} como el servicio real,
/// con un catálogo fijo tomado de los productos semilla de Products.API (ProductSeedData).
/// Reemplaza la red del cliente HTTP de Products, así los tests usan el ProductsClient real
/// (con su DelegatingHandler y su parseo) sin levantar Products.API.
/// </summary>
public class FakeProductsApi
{
    public static readonly Guid Notebook = Guid.Parse("3fa85f64-5717-4562-b3fc-2c963f66afa6");
    public static readonly Guid Auriculares = Guid.Parse("aaaabbbb-cccc-dddd-eeee-ffff00001111");
    public static readonly Guid Taladro = Guid.Parse("b2c3d4e5-0000-0000-0000-000000000005");

    private static readonly Dictionary<Guid, (string Nombre, decimal Precio, int Stock)> Catalogo = new()
    {
        [Notebook] = ("Notebook Dell XPS 15", 1500m, 10),
        [Auriculares] = ("Auriculares inalámbricos", 350m, 25),
        [Taladro] = ("Taladro percutor 750W", 120m, 2)
    };

    /// <summary>Si es true, toda llamada falla como si Products.API estuviera caído.</summary>
    public bool Caido { get; set; }

    /// <summary>Correlation ID recibido en cada llamada, en orden de llegada (null si no vino).</summary>
    public ConcurrentQueue<string?> CorrelationIdsRecibidos { get; } = new();

    /// <summary>Un handler nuevo por cliente: IHttpClientFactory descarta los handlers viejos.</summary>
    public HttpMessageHandler CrearHandler() => new Handler(this);

    private sealed class Handler(FakeProductsApi api) : HttpMessageHandler
    {
        private const string ProductsPath = "/api/products/";

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            api.CorrelationIdsRecibidos.Enqueue(
                request.Headers.TryGetValues("X-Correlation-Id", out var values) ? values.Single() : null);

            if (api.Caido)
            {
                throw new HttpRequestException("Connection refused (products.test)");
            }

            var path = request.RequestUri!.AbsolutePath;
            if (path == "/health/live")
            {
                return Task.FromResult(Json(HttpStatusCode.OK, new { status = "Healthy" }));
            }

            if (path.StartsWith(ProductsPath)
                && Guid.TryParse(path[ProductsPath.Length..], out var id)
                && Catalogo.TryGetValue(id, out var producto))
            {
                return Task.FromResult(Json(HttpStatusCode.OK, new
                {
                    id,
                    nombre = producto.Nombre,
                    precio = producto.Precio,
                    stock = producto.Stock,
                    categoria = "Electrónica",
                    fechaCreacion = new DateTime(2024, 1, 15, 10, 30, 0, DateTimeKind.Utc)
                }));
            }

            return Task.FromResult(Json(HttpStatusCode.NotFound, new { errorCode = "PRD-001" }));
        }

        private static HttpResponseMessage Json(HttpStatusCode status, object body) => new(status)
        {
            Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json")
        };
    }
}
