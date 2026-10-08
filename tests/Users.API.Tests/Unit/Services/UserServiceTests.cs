using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using Users.API.DTOs;
using Users.API.Exceptions;
using Users.API.Models;
using Users.API.Repositories;
using Users.API.Services;

namespace Users.API.Tests.Unit.Services;

public class UserServiceTests
{
    private static readonly DateTime Ahora = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

    private readonly IUserRepository _repository = Substitute.For<IUserRepository>();
    private readonly IPasswordHasher<User> _passwordHasher = Substitute.For<IPasswordHasher<User>>();
    private readonly UserService _sut;

    public UserServiceTests()
    {
        var timeProvider = new FakeTimeProvider(new DateTimeOffset(Ahora));
        _sut = new UserService(_repository, _passwordHasher, new AccountLockoutPolicy(), timeProvider);
    }

    // ---------- RegisterAsync ----------

    [Fact]
    public async Task RegisterAsync_DatosValidos_GuardaElUsuarioConLaPasswordHasheada()
    {
        var request = NuevoRegistro("maria@email.com");
        _passwordHasher.HashPassword(Arg.Any<User>(), "MiPassword123!").Returns("hash-generado");

        var response = await _sut.RegisterAsync(request);

        Assert.NotEqual(Guid.Empty, response.Id);
        Assert.Equal("María", response.Nombre);
        Assert.Equal("González", response.Apellido);
        Assert.Equal("maria@email.com", response.Email);
        Assert.Equal(Ahora, response.FechaRegistro);
        Assert.True(response.Activo);

        await _repository.Received(1).AgregarAsync(
            Arg.Is<User>(u => u.Id == response.Id
                              && u.PasswordHash == "hash-generado"
                              && u.Activo
                              && u.IntentosFallidos == 0),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RegisterAsync_EmailExistente_LanzaUsr001()
    {
        var request = NuevoRegistro("maria@email.com");
        _repository.ExisteEmailAsync(request.Email, Arg.Any<CancellationToken>()).Returns(true);

        var exception = await Assert.ThrowsAsync<BusinessRuleException>(() => _sut.RegisterAsync(request));

        Assert.Equal(ErrorCodes.USR_001, exception.ErrorCode);
        Assert.Equal(StatusCodes.Status409Conflict, exception.StatusCode);
        Assert.Equal("El email 'maria@email.com' ya está registrado.", exception.Message);
        await _repository.DidNotReceive().AgregarAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
    }

    // ---------- LoginAsync ----------

    [Fact]
    public async Task LoginAsync_CredencialesCorrectas_DevuelveElUsuarioYReseteaLosIntentos()
    {
        var user = Usuario(activo: true, intentosFallidos: 2);
        ConfigurarLogin(user, PasswordVerificationResult.Success);

        var response = await _sut.LoginAsync(Login(user.Email));

        Assert.Equal(user.Id, response.Id);
        Assert.Equal(user.Nombre, response.Nombre);
        Assert.Equal(user.Email, response.Email);
        Assert.Equal(0, user.IntentosFallidos);
        await _repository.Received(1).ActualizarAsync(user, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task LoginAsync_EmailInexistente_LanzaUsr003()
    {
        _repository.ObtenerPorEmailAsync("nadie@email.com", Arg.Any<CancellationToken>()).Returns((User?)null);

        var exception = await Assert.ThrowsAsync<BusinessRuleException>(
            () => _sut.LoginAsync(Login("nadie@email.com")));

        Assert.Equal(ErrorCodes.USR_003, exception.ErrorCode);
        Assert.Equal(StatusCodes.Status401Unauthorized, exception.StatusCode);
        Assert.Equal("Credenciales incorrectas.", exception.Message);
    }

    [Fact]
    public async Task LoginAsync_PasswordIncorrecta_LanzaUsr003YSumaUnIntento()
    {
        var user = Usuario(activo: true, intentosFallidos: 0);
        ConfigurarLogin(user, PasswordVerificationResult.Failed);

        var exception = await Assert.ThrowsAsync<BusinessRuleException>(
            () => _sut.LoginAsync(Login(user.Email)));

        Assert.Equal(ErrorCodes.USR_003, exception.ErrorCode);
        Assert.Equal(StatusCodes.Status401Unauthorized, exception.StatusCode);
        Assert.Equal(1, user.IntentosFallidos);
        Assert.True(user.Activo);
        await _repository.Received(1).ActualizarAsync(user, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task LoginAsync_TercerIntentoFallido_BloqueaLaCuentaYLanzaUsr003()
    {
        var user = Usuario(activo: true, intentosFallidos: 2);
        ConfigurarLogin(user, PasswordVerificationResult.Failed);

        var exception = await Assert.ThrowsAsync<BusinessRuleException>(
            () => _sut.LoginAsync(Login(user.Email)));

        // D-09: el tercer intento responde USR-003 y deja la cuenta bloqueada.
        Assert.Equal(ErrorCodes.USR_003, exception.ErrorCode);
        Assert.Equal(StatusCodes.Status401Unauthorized, exception.StatusCode);
        Assert.Equal(3, user.IntentosFallidos);
        Assert.False(user.Activo);
    }

    [Fact]
    public async Task LoginAsync_BloqueadoPorIntentos_AunConPasswordCorrecta_LanzaUsr004()
    {
        var user = Usuario(activo: false, intentosFallidos: 3);
        ConfigurarLogin(user, PasswordVerificationResult.Success);

        var exception = await Assert.ThrowsAsync<BusinessRuleException>(
            () => _sut.LoginAsync(Login(user.Email)));

        Assert.Equal(ErrorCodes.USR_004, exception.ErrorCode);
        Assert.Equal(StatusCodes.Status403Forbidden, exception.StatusCode);
        Assert.Equal(
            "Su cuenta fue bloqueada por superar el máximo de intentos fallidos. Contacte a soporte.",
            exception.Message);
        _passwordHasher.DidNotReceive()
            .VerifyHashedPassword(Arg.Any<User>(), Arg.Any<string>(), Arg.Any<string>());
    }

    [Fact]
    public async Task LoginAsync_BloqueadoManualmente_LanzaUsr005()
    {
        var user = Usuario(activo: false, intentosFallidos: 0);
        ConfigurarLogin(user, PasswordVerificationResult.Success);

        var exception = await Assert.ThrowsAsync<BusinessRuleException>(
            () => _sut.LoginAsync(Login(user.Email)));

        Assert.Equal(ErrorCodes.USR_005, exception.ErrorCode);
        Assert.Equal(StatusCodes.Status403Forbidden, exception.StatusCode);
        Assert.Equal(
            "Su cuenta fue suspendida por razones de seguridad. Contacte a soporte.",
            exception.Message);
    }

    // ---------- GetByIdAsync ----------

    [Fact]
    public async Task GetByIdAsync_UsuarioExistente_DevuelveElUsuario()
    {
        var user = Usuario(activo: true, intentosFallidos: 0);
        _repository.ObtenerPorIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);

        var response = await _sut.GetByIdAsync(user.Id);

        Assert.Equal(user.Id, response.Id);
        Assert.Equal(user.Email, response.Email);
        Assert.Equal(Ahora, response.FechaRegistro);
        Assert.True(response.Activo);
    }

    [Fact]
    public async Task GetByIdAsync_UsuarioInexistente_LanzaUsr007()
    {
        var id = Guid.NewGuid();
        _repository.ObtenerPorIdAsync(id, Arg.Any<CancellationToken>()).Returns((User?)null);

        var exception = await Assert.ThrowsAsync<NotFoundException>(() => _sut.GetByIdAsync(id));

        Assert.Equal(ErrorCodes.USR_007, exception.ErrorCode);
        Assert.Equal("Usuario no encontrado.", exception.Message);
    }

    // ---------- Ayudas ----------

    private void ConfigurarLogin(User user, PasswordVerificationResult resultado)
    {
        _repository.ObtenerPorEmailAsync(user.Email, Arg.Any<CancellationToken>()).Returns(user);
        _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, Arg.Any<string>()).Returns(resultado);
    }

    private static RegisterUserRequest NuevoRegistro(string email) => new()
    {
        Nombre = "María",
        Apellido = "González",
        Email = email,
        Password = "MiPassword123!"
    };

    private static LoginRequest Login(string email) => new() { Email = email, Password = "MiPassword123!" };

    private static User Usuario(bool activo, int intentosFallidos) => new()
    {
        Id = Guid.NewGuid(),
        Nombre = "María",
        Apellido = "González",
        Email = "maria@email.com",
        PasswordHash = "hash",
        FechaRegistro = Ahora,
        Activo = activo,
        IntentosFallidos = intentosFallidos
    };
}