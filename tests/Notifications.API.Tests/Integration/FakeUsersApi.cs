using System.Collections.Concurrent;
using System.Net;
using System.Text;
using System.Text.Json;

namespace Notifications.API.Tests.Integration;

/// <summary>
/// Users.API falso: responde GET /health/live y GET /api/users/{id} como el servicio real.
/// Solo existe María, la misma usuaria semilla de Users.API (UserSeedData).
/// Reemplaza la red del cliente HTTP de Users, así los tests usan el UsersClient real
/// (con su DelegatingHandler y su parseo) sin levantar Users.API.
/// </summary>
public class FakeUsersApi
{
    public static readonly Guid Maria = Guid.Parse("a1b2c3d4-0000-0000-0000-111122223333");

    /// <summary>Si es true, toda llamada falla como si Users.API estuviera caído.</summary>
    public bool Caido { get; set; }

    /// <summary>Correlation ID recibido en cada llamada, en orden de llegada (null si no vino).</summary>
    public ConcurrentQueue<string?> CorrelationIdsRecibidos { get; } = new();

    /// <summary>Un handler nuevo por cliente: IHttpClientFactory descarta los handlers viejos.</summary>
    public HttpMessageHandler CrearHandler() => new Handler(this);

    private sealed class Handler(FakeUsersApi api) : HttpMessageHandler
    {
        private const string UsersPath = "/api/users/";

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            api.CorrelationIdsRecibidos.Enqueue(
                request.Headers.TryGetValues("X-Correlation-Id", out var values) ? values.Single() : null);

            if (api.Caido)
            {
                throw new HttpRequestException("Connection refused (users.test)");
            }

            var path = request.RequestUri!.AbsolutePath;
            if (path == "/health/live")
            {
                return Task.FromResult(Json(HttpStatusCode.OK, new { status = "Healthy" }));
            }

            if (path.StartsWith(UsersPath)
                && Guid.TryParse(path[UsersPath.Length..], out var id)
                && id == Maria)
            {
                return Task.FromResult(Json(HttpStatusCode.OK, new
                {
                    id,
                    nombre = "María",
                    apellido = "González",
                    email = "maria@email.com",
                    fechaRegistro = new DateTime(2024, 3, 10, 9, 0, 0, DateTimeKind.Utc),
                    activo = true
                }));
            }

            return Task.FromResult(Json(HttpStatusCode.NotFound, new { errorCode = "USR-007" }));
        }

        private static HttpResponseMessage Json(HttpStatusCode status, object body) => new(status)
        {
            Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json")
        };
    }
}
