using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Orders.API.Exceptions;
using Orders.API.Services;
using Serilog.Core;
using Serilog.Events;

namespace Orders.API.Tests.Integration;

/// <summary>
/// Requisitos de la sección 5.3 del enunciado: cada log con Timestamp, Nivel, Servicio, Endpoint,
/// Correlation ID y errorCode cuando aplique; inicio y fin de cada request con duración;
/// errores de negocio como Warning e inesperados como Error.
/// </summary>
public class LoggingTests(OrdersApiFactory factory) : IClassFixture<OrdersApiFactory>
{
    [Fact]
    public async Task Request_Exitoso_LogueaInicioYFinConDuracionYPropiedadesComunes()
    {
        var (client, sink) = ClienteConSink();

        var response = await client.SendAsync(ConCorrelationId(HttpMethod.Get, "/api/orders", "log-ok"));
        await response.Content.ReadAsStringAsync();

        var inicio = await sink.WaitForAsync(e => e.Property("CorrelationId") == "log-ok" && e.MessageTemplate.Text.StartsWith("Inicio"));
        var fin = await sink.WaitForAsync(e => e.Property("CorrelationId") == "log-ok" && e.MessageTemplate.Text.StartsWith("Fin"));

        foreach (var evento in new[] { inicio, fin })
        {
            Assert.Equal(LogEventLevel.Information, evento.Level);
            Assert.Equal("Orders.API", evento.Property("Servicio"));
            Assert.Equal("GET /api/orders", evento.Property("Endpoint"));
        }
        Assert.Equal("200", fin.Property("StatusCode"));
        Assert.NotNull(fin.Property("ElapsedMs"));
    }

    [Fact]
    public async Task ErrorDeNegocio_SeLogueaComoWarningConErrorCode()
    {
        var (client, sink) = ClienteConSink();

        await client.SendAsync(ConCorrelationId(HttpMethod.Get, "/api/orders/abc", "log-warning"));

        var evento = await sink.WaitForAsync(e => e.Property("CorrelationId") == "log-warning" && e.Property("ErrorCode") is not null && e.Level >= LogEventLevel.Warning);
        Assert.Equal(LogEventLevel.Warning, evento.Level);
        Assert.Equal(ErrorCodes.ORD_001, evento.Property("ErrorCode"));
        Assert.Equal("GET /api/orders/abc", evento.Property("Endpoint"));
    }

    [Fact]
    public async Task ErrorDeNegocio_ElLogDeFinIncluyeElErrorCode()
    {
        var (client, sink) = ClienteConSink();

        await client.SendAsync(ConCorrelationId(HttpMethod.Get, "/api/orders/abc", "log-fin-error"));

        var fin = await sink.WaitForAsync(e => e.Property("CorrelationId") == "log-fin-error" && e.MessageTemplate.Text.StartsWith("Fin"));
        Assert.Equal("404", fin.Property("StatusCode"));
        Assert.Equal(ErrorCodes.ORD_001, fin.Property("ErrorCode"));
    }

    [Fact]
    public async Task ErrorInesperado_SeLogueaComoErrorConLaExcepcionYORD007()
    {
        var service = Substitute.For<IOrderService>();
        service.GetAllAsync(Arg.Any<Guid?>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("Falla simulada"));
        var (client, sink) = ClienteConSink(s => s.AddScoped(_ => service));

        await client.SendAsync(ConCorrelationId(HttpMethod.Get, "/api/orders", "log-error"));

        var evento = await sink.WaitForAsync(e => e.Property("CorrelationId") == "log-error" && e.Level == LogEventLevel.Error);
        Assert.Equal(ErrorCodes.ORD_007, evento.Property("ErrorCode"));
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
