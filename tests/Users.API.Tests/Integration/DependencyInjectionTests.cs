using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Users.API.Repositories;
using Users.API.Services;

namespace Users.API.Tests.Integration;

/// <summary>
/// Verifica la configuración REAL del contenedor: los tests unitarios crean las clases a mano
/// y no detectan un registro faltante.
/// </summary>
public class DependencyInjectionTests : IDisposable
{
    private readonly WebApplicationFactory<Program> _factory =
        new WebApplicationFactory<Program>().WithWebHostBuilder(b => b.UseSetting("LogFile:Enabled", "false"));

    [Fact]
    public void UserService_ConLaConfiguracionDeLaApp_SeResuelveConTodasSusDependencias()
    {
        using var scope = _factory.Services.CreateScope();

        Assert.IsType<UserService>(scope.ServiceProvider.GetRequiredService<IUserService>());
    }

    [Fact]
    public async Task UserRepository_EsSingletonYTraeLosUsuariosSemilla()
    {
        var repository = _factory.Services.GetRequiredService<IUserRepository>();

        Assert.Same(repository, _factory.Services.GetRequiredService<IUserRepository>());
        Assert.NotNull(await repository.GetByIdAsync(Guid.Parse("a1b2c3d4-0000-0000-0000-111122223333")));
    }

    public void Dispose() => _factory.Dispose();
}
