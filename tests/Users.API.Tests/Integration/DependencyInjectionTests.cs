using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
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
    // Base SQLite temporal propia: la configuración es la real, salvo dónde se guarda la base y el archivo de log,
    // así el test no crea ni toca la base de la app (src/Users.API/users.db).
    private readonly string _databasePath = Path.Combine(Path.GetTempPath(), $"users-tests-{Guid.NewGuid():N}.db");

    private readonly WebApplicationFactory<Program> _factory;

    public DependencyInjectionTests() =>
        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(b => b
            .UseSetting("LogFile:Enabled", "false")
            .UseSetting("ConnectionStrings:DefaultConnection", $"Data Source={_databasePath}"));

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
