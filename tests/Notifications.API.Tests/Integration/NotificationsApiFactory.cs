using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Notifications.API.Clients;

namespace Notifications.API.Tests.Integration;

/// <summary>
/// Levanta Notifications.API en memoria con Users.API reemplazado por <see cref="FakeUsersClient"/>,
/// para no depender de que Users esté levantado.
/// </summary>
public class NotificationsApiFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("LogFile:Enabled", "false");
        builder.ConfigureTestServices(services => services.AddSingleton<IUsersClient, FakeUsersClient>());
    }
}

/// <summary>
/// Solo existe María, la misma usuaria semilla de Users.API (UserSeedData).
/// </summary>
public class FakeUsersClient : IUsersClient
{
    public static readonly Guid Maria = Guid.Parse("a1b2c3d4-0000-0000-0000-111122223333");

    public Task<UserInfo?> GetUserAsync(Guid userId, CancellationToken cancellationToken = default) =>
        Task.FromResult<UserInfo?>(userId == Maria
            ? new UserInfo
            {
                Id = Maria,
                Nombre = "María",
                Apellido = "González",
                Email = "maria@email.com",
                Activo = true
            }
            : null);
}