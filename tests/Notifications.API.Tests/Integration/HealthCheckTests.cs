using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Notifications.API.Repositories;

namespace Notifications.API.Tests.Integration;

/// <summary>
/// Requisitos de la sección 5.4 del enunciado. La dependencia con Users.API en /health/ready
/// se agrega en la Etapa 9.
/// </summary>
public class HealthCheckTests(NotificationsApiFactory factory) : IClassFixture<NotificationsApiFactory>
{
    [Theory]
    [InlineData("/health")]
    [InlineData("/health/ready")]
    [InlineData("/health/live")]
    public async Task HealthCheck_ServicioSano_Devuelve200ConJsonHealthy(string url)
    {
        var response = await factory.CreateClient().GetAsync(url);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("Healthy", body.RootElement.GetProperty("status").GetString());
        Assert.Equal("Notifications.API", body.RootElement.GetProperty("servicio").GetString());
        Assert.NotEmpty(body.RootElement.GetProperty("checks").EnumerateArray());
    }

    [Fact]
    public async Task Ready_PersistenciaCaida_Devuelve503YLiveSigueEn200()
    {
        var repository = Substitute.For<INotificationRepository>();
        repository.GetByUserIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).ThrowsAsync(new InvalidOperationException("Sin conexión"));
        var client = factory.WithWebHostBuilder(b => b.ConfigureTestServices(s => s.AddSingleton(repository))).CreateClient();

        var ready = await client.GetAsync("/health/ready");
        var live = await client.GetAsync("/health/live");

        Assert.Equal(HttpStatusCode.ServiceUnavailable, ready.StatusCode);
        Assert.Equal(HttpStatusCode.OK, live.StatusCode);
    }
}
