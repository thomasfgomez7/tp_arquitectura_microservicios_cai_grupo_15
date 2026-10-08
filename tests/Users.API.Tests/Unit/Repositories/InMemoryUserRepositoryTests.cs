using Users.API.Models;
using Users.API.Repositories;

namespace Users.API.Tests.Unit.Repositories;

public class InMemoryUserRepositoryTests
{
    [Fact]
    public async Task ExisteEmailAsync_SinDistinguirMayusculas_DevuelveTrue()
    {
        var repository = new InMemoryUserRepository([Usuario("maria@email.com")]);

        Assert.True(await repository.ExisteEmailAsync("MARIA@email.com"));
        Assert.False(await repository.ExisteEmailAsync("otro@email.com"));
    }

    [Fact]
    public async Task ObtenerPorIdAsync_UsuarioExistente_LoDevuelve()
    {
        var user = Usuario("maria@email.com");
        var repository = new InMemoryUserRepository([user]);

        Assert.Same(user, await repository.ObtenerPorIdAsync(user.Id));
        Assert.Null(await repository.ObtenerPorIdAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task AgregarAsync_UsuarioNuevo_SePuedeBuscarPorEmail()
    {
        var repository = new InMemoryUserRepository();
        var user = Usuario("nuevo@email.com");

        await repository.AgregarAsync(user);

        Assert.Same(user, await repository.ObtenerPorEmailAsync("NUEVO@email.com"));
    }

    [Fact]
    public async Task AgregarAsync_EmailRepetido_LanzaInvalidOperationException()
    {
        var repository = new InMemoryUserRepository([Usuario("maria@email.com")]);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => repository.AgregarAsync(Usuario("Maria@Email.com")));
    }

    private static User Usuario(string email) => new()
    {
        Id = Guid.NewGuid(),
        Nombre = "María",
        Apellido = "González",
        Email = email,
        PasswordHash = "hash",
        Activo = true
    };
}