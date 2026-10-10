using System.Net;
using NSubstitute;
using Products.API.Infrastructure;

namespace Products.API.Tests.Unit.Infrastructure;

public class CorrelationIdDelegatingHandlerTests
{
    private const string Header = "X-Correlation-Id";

    private readonly ICorrelationIdAccessor _accessor = Substitute.For<ICorrelationIdAccessor>();
    private readonly FakeHttpHandler _red = new(HttpStatusCode.OK);

    [Fact]
    public async Task SendAsync_ConUnRequestEnCurso_AgregaSuCorrelationIdALaLlamadaSaliente()
    {
        _accessor.CorrelationId.Returns("demo-123");

        await CrearCliente().GetAsync("api/orders");

        Assert.Equal("demo-123", _red.UltimoRequest!.Headers.GetValues(Header).Single());
    }

    [Fact]
    public async Task SendAsync_SinRequestEnCurso_NoAgregaElHeader()
    {
        // Ej.: el health check que corre fuera de un request de la API.
        _accessor.CorrelationId.Returns((string?)null);

        await CrearCliente().GetAsync("api/orders");

        Assert.False(_red.UltimoRequest!.Headers.Contains(Header));
    }

    [Fact]
    public async Task SendAsync_ConElHeaderYaPuesto_NoLoDuplica()
    {
        _accessor.CorrelationId.Returns("demo-123");
        var request = new HttpRequestMessage(HttpMethod.Get, "api/orders");
        request.Headers.Add(Header, "otro-id");

        await CrearCliente().SendAsync(request);

        Assert.Equal("otro-id", _red.UltimoRequest!.Headers.GetValues(Header).Single());
    }

    private HttpClient CrearCliente() =>
        new(new CorrelationIdDelegatingHandler(_accessor) { InnerHandler = _red })
        {
            BaseAddress = new Uri("http://orders.test/")
        };
}
