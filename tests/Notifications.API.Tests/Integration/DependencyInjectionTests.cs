using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using Notifications.API.Clients;
using Notifications.API.Services;

namespace Notifications.API.Tests.Integration;

/// <summary>
/// Verifica la configuración REAL del contenedor, sin los reemplazos de NotificationsApiFactory.
/// </summary>
public class DependencyInjectionTests : IDisposable
{
    // Base SQLite temporal propia: la configuración es la real, salvo dónde se guarda la base y el archivo de log,
    // así el test no crea ni toca la base de la app (src/Notifications.API/notifications.db).
    private readonly string _databasePath = Path.Combine(Path.GetTempPath(), $"notifications-tests-{Guid.NewGuid():N}.db");

    private readonly WebApplicationFactory<Program> _factory;

    public DependencyInjectionTests() =>
        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(b => b
            .UseSetting("LogFile:Enabled", "false")
            .UseSetting("ConnectionStrings:DefaultConnection", $"Data Source={_databasePath}"));

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

    public void Dispose()
    {
        _factory.Dispose();
        SqliteConnection.ClearAllPools();
        try
        {
            File.Delete(_databasePath);
        }
        catch (IOException)
        {
            // Es un archivo temporal: si sigue en uso, lo limpia el sistema.
        }
    }
}