using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Notifications.API.Clients;

namespace Notifications.API.Tests.Integration;

/// <summary>
/// Levanta Notifications.API en memoria para los tests de integración: sin archivo de log y con la red hacia
/// Users.API reemplazada por <see cref="FakeUsersApi"/>, para no depender de que Users esté levantado.
/// </summary>
public class NotificationsApiFactory : WebApplicationFactory<Program>
{
    public FakeUsersApi UsersApi { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("LogFile:Enabled", "false");

        // Solo se reemplaza el último eslabón (la red): el UsersClient, su DelegatingHandler
        // y el health check de Users son los reales.
        builder.ConfigureTestServices(services =>
            services.AddHttpClient(nameof(IUsersClient)).ConfigurePrimaryHttpMessageHandler(UsersApi.CrearHandler));
    }
}
