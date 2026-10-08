using System.Net.Http.Json;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Notifications.API.Exceptions;
using Notifications.API.Services;
using Serilog.Core;
using Serilog.Events;

namespace Notifications.API.Tests.Integration;

/// <summary>
/// Requisitos de la sección 5.3 del enunciado: cada log con Timestamp, Nivel, Servicio, Endpoint,
/// Correlation ID y errorCode cuando aplique; inicio y fin de cada request con duración;
/// errores de negocio como Warning e inesperados como Error.
/// </summary>
public class LoggingTests(NotificationsApiFactory factory) : IClassFixture<NotificationsApiFactory>
{
    private const string SendUrl = "/api/notifications/send";

    [Fact]
    public async Task Request_Exitoso_LogueaInicioYFinConDuracionYPropiedadesComunes()
    {
        var (client, sink) = ClienteConSink();
        var request = ConCorrelationId(HttpMethod.Post, SendUrl, "log-ok");
        request.Content = JsonContent.Create(new { usuarioId = FakeUsersClient.Maria, mensaje = "Hola", tipo = "Email" });

        var response = await client.SendAsync(request);
        await response.Content.ReadAsStringAsync();

        var inicio = await sink.WaitForAsync(e => e.Property("CorrelationId") == "log-ok" && e.MessageTemplate.Text.StartsWith("Inicio"));
        var fin = await sink.WaitForAsync(e => e.Property("CorrelationId") == "log-ok" && e.MessageTemplate.Text.StartsWith("Fin"));

        foreach (var evento in new[] { inicio, fin })
        {
            Assert.Equal(LogEventLevel.Information, evento.Level);
            Assert.Equal("Notifications.API", evento.Property("Servicio"));
            Assert.Equal($"POST {SendUrl}", evento.Property("Endpoint"));
        }
        Assert.Equal("201", fin.Property("StatusCode"));
        Assert.NotNull(fin.Property("ElapsedMs"));
    }

    [Fact]
    public async Task ErrorDeNegocio_SeLogueaComoWarningConErrorCode()
    {
        var (client, sink) = ClienteConSink();

        await client.SendAsync(ConCorrelationId(HttpMethod.Get, "/api/notifications/abc", "log-warning"));

        var evento = await sink.WaitForAsync(e => e.Property("CorrelationId") == "log-warning" && e.Property("ErrorCode") is not null && e.Level >= LogEventLevel.Warning);
        Assert.Equal(LogEventLevel.Warning, evento.Level);
        Assert.Equal(ErrorCodes.NTF_003, evento.Property("ErrorCode"));
        Assert.Equal("GET /api/notifications/abc", evento.Property("Endpoint"));
    }

    [Fact]
    public async Task ErrorDeNegocio_ElLogDeFinIncluyeElErrorCode()
    {
        var (client, sink) = ClienteConSink();

        await client.SendAsync(ConCorrelationId(HttpMethod.Get, "/api/notifications/abc", "log-fin-error"));

        var fin = await sink.WaitForAsync(e => e.Property("CorrelationId") == "log-fin-error" && e.MessageTemplate.Text.StartsWith("Fin"));
        Assert.Equal("404", fin.Property("StatusCode"));
        Assert.Equal(ErrorCodes.NTF_003, fin.Property("ErrorCode"));
    }

    [Fact]
    public async Task ErrorInesperado_SeLogueaComoErrorConLaExcepcionYNTF004()
    {
        var service = Substitute.For<INotificationService>();
        service.GetByUserIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("Falla simulada"));
        var (client, sink) = ClienteConSink(s => s.AddScoped(_ => service));

        await client.SendAsync(ConCorrelationId(HttpMethod.Get, $"/api/notifications/{FakeUsersClient.Maria}", "log-error"));

        var evento = await sink.WaitForAsync(e => e.Property("CorrelationId") == "log-error" && e.Level == LogEventLevel.Error);
        Assert.Equal(ErrorCodes.NTF_004, evento.Property("ErrorCode"));
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
