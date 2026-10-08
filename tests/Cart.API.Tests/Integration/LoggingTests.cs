using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Cart.API.Exceptions;
using Cart.API.Services;
using Serilog.Core;
using Serilog.Events;

namespace Cart.API.Tests.Integration;

/// <summary>
/// Requisitos de la sección 5.3 del enunciado: cada log con Timestamp, Nivel, Servicio, Endpoint,
/// Correlation ID y errorCode cuando aplique; inicio y fin de cada request con duración;
/// errores de negocio como Warning e inesperados como Error.
/// </summary>
public class LoggingTests(CartApiFactory factory) : IClassFixture<CartApiFactory>
{
    [Fact]
    public async Task Request_Exitoso_LogueaInicioYFinConDuracionYPropiedadesComunes()
    {
        var (client, sink) = ClienteConSink();

        var response = await client.SendAsync(ConCorrelationId(HttpMethod.Get, "/api/cart/a1b2c3d4-0000-0000-0000-111122223333", "log-ok"));
        await response.Content.ReadAsStringAsync();

        var inicio = await sink.WaitForAsync(e => e.Property("CorrelationId") == "log-ok" && e.MessageTemplate.Text.StartsWith("Inicio"));
        var fin = await sink.WaitForAsync(e => e.Property("CorrelationId") == "log-ok" && e.MessageTemplate.Text.StartsWith("Fin"));

        foreach (var evento in new[] { inicio, fin })
        {
            Assert.Equal(LogEventLevel.Information, evento.Level);
            Assert.Equal("Cart.API", evento.Property("Servicio"));
            Assert.Equal("GET /api/cart/a1b2c3d4-0000-0000-0000-111122223333", evento.Property("Endpoint"));
        }
        Assert.Equal("200", fin.Property("StatusCode"));
        Assert.NotNull(fin.Property("ElapsedMs"));
    }

    [Fact]
    public async Task ErrorDeNegocio_SeLogueaComoWarningConErrorCode()
    {
        var (client, sink) = ClienteConSink();

        await client.SendAsync(ConCorrelationId(HttpMethod.Get, "/api/cart/abc", "log-warning"));

        var evento = await sink.WaitForAsync(e => e.Property("CorrelationId") == "log-warning" && e.Property("ErrorCode") is not null && e.Level >= LogEventLevel.Warning);
        Assert.Equal(LogEventLevel.Warning, evento.Level);
        Assert.Equal(ErrorCodes.CRT_001, evento.Property("ErrorCode"));
        Assert.Equal("GET /api/cart/abc", evento.Property("Endpoint"));
    }

    [Fact]
    public async Task ErrorDeNegocio_ElLogDeFinIncluyeElErrorCode()
    {
        var (client, sink) = ClienteConSink();

        await client.SendAsync(ConCorrelationId(HttpMethod.Get, "/api/cart/abc", "log-fin-error"));

        var fin = await sink.WaitForAsync(e => e.Property("CorrelationId") == "log-fin-error" && e.MessageTemplate.Text.StartsWith("Fin"));
        Assert.Equal("404", fin.Property("StatusCode"));
        Assert.Equal(ErrorCodes.CRT_001, fin.Property("ErrorCode"));
    }

    [Fact]
    public async Task ErrorInesperado_SeLogueaComoErrorConLaExcepcionYCRT005()
    {
        var service = Substitute.For<ICartService>();
        service.GetAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("Falla simulada"));
        var (client, sink) = ClienteConSink(s => s.AddScoped(_ => service));

        await client.SendAsync(ConCorrelationId(HttpMethod.Get, "/api/cart/a1b2c3d4-0000-0000-0000-111122223333", "log-error"));

        var evento = await sink.WaitForAsync(e => e.Property("CorrelationId") == "log-error" && e.Level == LogEventLevel.Error);
        Assert.Equal(ErrorCodes.CRT_005, evento.Property("ErrorCode"));
        Assert.IsType<InvalidOperationException>(evento.Exception);
    }

    private (HttpClient Client, CollectingSink Sink) ClienteConSink(Action<IServiceCollection>? configurar = null)
    {
        var sink = new CollectingSink();
        var client = factory
            .WithWebHostBuilder(b => b.ConfigureTestServices(s =>
            {
                s.AddSingleton<ILogEventSink>(sink);
                configurar?.Invoke(s);
            }))
            .CreateClient();
        return (client, sink);
    }

    private static HttpRequestMessage ConCorrelationId(HttpMethod method, string url, string correlationId)
    {
        var request = new HttpRequestMessage(method, url);
        request.Headers.Add("X-Correlation-Id", correlationId);
        return request;
    }
}
