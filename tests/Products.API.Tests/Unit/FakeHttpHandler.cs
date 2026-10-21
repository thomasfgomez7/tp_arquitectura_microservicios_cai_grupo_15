using System.Net;
using System.Text;

namespace Products.API.Tests.Unit;

/// <summary>
/// Reemplaza la red en los tests unitarios: devuelve una respuesta fija (o lanza una excepción)
/// y guarda el último request recibido.
/// </summary>
public sealed class FakeHttpHandler : HttpMessageHandler
{
    private readonly HttpStatusCode _status;
    private readonly string _body = "";
    private readonly Exception? _error;

    public FakeHttpHandler(HttpStatusCode status, string body = "") => (_status, _body) = (status, body);

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
