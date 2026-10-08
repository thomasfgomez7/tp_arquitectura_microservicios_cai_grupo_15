using Users.API.Models;
using Users.API.Repositories;

namespace Users.API.Tests.Unit.Repositories;

public class InMemoryUserRepositoryTests
{
    [Fact]
    public async Task ExistsByEmailAsync_SinDistinguirMayusculas_DevuelveTrue()
    {
        var repository = new InMemoryUserRepository([Usuario("maria@email.com")]);

        Assert.True(await repository.ExistsByEmailAsync("MARIA@email.com"));
        Assert.False(await repository.ExistsByEmailAsync("otro@email.com"));
    }

    [Fact]
    public async Task GetByIdAsync_UsuarioExistente_LoDevuelve()
    {
        var user = Usuario("maria@email.com");
        var repository = new InMemoryUserRepository([user]);

        Assert.Same(user, await repository.GetByIdAsync(user.Id));
        Assert.Null(await repository.GetByIdAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task AddAsync_UsuarioNuevo_SePuedeBuscarPorEmail()
    {
        var repository = new InMemoryUserRepository();
        var user = Usuario("nuevo@email.com");

        await repository.AddAsync(user);

        Assert.Same(user, await repository.GetByEmailAsync("NUEVO@email.com"));
    }

    [Fact]
    public async Task AddAsync_EmailRepetido_LanzaInvalidOperationException()
    {
        var repository = new InMemoryUserRepository([Usuario("maria@email.com")]);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => repository.AddAsync(Usuario("Maria@Email.com")));
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