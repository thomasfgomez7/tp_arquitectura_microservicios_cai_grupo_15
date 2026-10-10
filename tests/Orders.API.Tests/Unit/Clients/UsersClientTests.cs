using System.Net;
using System.Text;
using Orders.API.Clients;

namespace Orders.API.Tests.Unit.Clients;

public class UsersClientTests
{
    private static readonly Guid UsuarioId = Guid.Parse("a1b2c3d4-0000-0000-0000-111122223333");

    [Fact]
    public async Task GetUserAsync_UsersResponde200_DevuelveElUsuario()
    {
        var handler = new FakeHttpHandler(HttpStatusCode.OK, """
            {
              "id": "a1b2c3d4-0000-0000-0000-111122223333",
              "nombre": "María",
              "apellido": "González",
              "email": "maria@email.com",
              "fechaRegistro": "2024-03-10T09:00:00Z",
              "activo": true
            }
            """);

        var usuario = await CrearCliente(handler).GetUserAsync(UsuarioId);

        Assert.NotNull(usuario);
        Assert.Equal(UsuarioId, usuario.Id);
        Assert.Equal("María", usuario.Nombre);
        Assert.Equal("maria@email.com", usuario.Email);
        Assert.True(usuario.Activo);
    }

    [Fact]
    public async Task GetUserAsync_LlamaAlEndpointDelContrato()
    {
        var handler = new FakeHttpHandler(HttpStatusCode.NotFound, "");

        await CrearCliente(handler).GetUserAsync(UsuarioId);

        Assert.Equal(HttpMethod.Get, handler.UltimoRequest!.Method);
        Assert.Equal($"http://users.test/api/users/{UsuarioId}", handler.UltimoRequest.RequestUri!.ToString());
    }

    [Fact]
    public async Task GetUserAsync_UsersResponde404_DevuelveNull()
    {
        // El body del 404 es el JSON de error de Users: no se intenta leer como usuario.
        var handler = new FakeHttpHandler(HttpStatusCode.NotFound, """{ "errorCode": "USR-007" }""");

        var usuario = await CrearCliente(handler).GetUserAsync(UsuarioId);

        Assert.Null(usuario);
    }

    [Theory]
    [InlineData(HttpStatusCode.InternalServerError)]
    [InlineData(HttpStatusCode.ServiceUnavailable)]
    [InlineData(HttpStatusCode.BadRequest)]
    public async Task GetUserAsync_UsersRespondeOtroError_LanzaHttpRequestException(HttpStatusCode status)
    {
        // La excepción llega al GlobalExceptionHandler y termina en un 500 con ORD-007 (D-36).
        var handler = new FakeHttpHandler(status, """{ "errorCode": "USR-006" }""");

        await Assert.ThrowsAsync<HttpRequestException>(() => CrearCliente(handler).GetUserAsync(UsuarioId));
    }

    [Fact]
    public async Task GetUserAsync_UsersNoResponde_PropagaLaExcepcion()
    {
        var handler = new FakeHttpHandler(new HttpRequestException("Connection refused"));

        await Assert.ThrowsAsync<HttpRequestException>(() => CrearCliente(handler).GetUserAsync(UsuarioId));
    }

    private static UsersClient CrearCliente(FakeHttpHandler handler) =>
        new(new HttpClient(handler) { BaseAddress = new Uri("http://users.test/") });

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
