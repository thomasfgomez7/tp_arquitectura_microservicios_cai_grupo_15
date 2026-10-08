using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Notifications.API.Clients;
using Notifications.API.Services;

namespace Notifications.API.Tests.Integration;

/// <summary>
/// Verifica la configuración REAL del contenedor, sin los reemplazos de NotificationsApiFactory.
/// </summary>
public class DependencyInjectionTests : IDisposable
{
    private readonly WebApplicationFactory<Program> _factory = new();

    [Fact]
    public void NotificationService_ConLaConfiguracionDeLaApp_SeResuelveConTodasSusDependencias()
    {
        using var scope = _factory.Services.CreateScope();

        Assert.IsType<NotificationService>(scope.ServiceProvider.GetRequiredService<INotificationService>());
    }

    [Fact]
    public void UsersClient_SeRegistraComoTypedClientConLaUrlDeUsers()
    {
        using var scope = _factory.Services.CreateScope();

        Assert.IsType<UsersClient>(scope.ServiceProvider.GetRequiredService<IUsersClient>());

        // AddHttpClient<IUsersClient, UsersClient> registra un cliente con el nombre de la interfaz.
        var httpClient = scope.ServiceProvider.GetRequiredService<IHttpClientFactory>()
            .CreateClient(nameof(IUsersClient));
        Assert.Equal(new Uri("http://localhost:5002/"), httpClient.BaseAddress);
        Assert.Equal(TimeSpan.FromSeconds(5), httpClient.Timeout);
    }

    public void Dispose() => _factory.Dispose();
}