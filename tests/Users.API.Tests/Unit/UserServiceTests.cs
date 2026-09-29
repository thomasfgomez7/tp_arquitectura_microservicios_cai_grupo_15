using NSubstitute;
using Microsoft.AspNetCore.Identity;
using Users.API.Services;
using Users.API.Repositories;
using Users.API.Models;
using Users.API.DTOs;
using Users.API.Exceptions;

namespace Users.API.Tests.Unit;

public class UserServiceTests
{
    private readonly IUserRepository _repositoryMock;
    private readonly IPasswordHasher<User> _passwordHasherMock;
    private readonly AccountLockoutPolicy _lockoutPolicy;
    private readonly UserService _sut;

    public UserServiceTests()
    {
        _repositoryMock = Substitute.For<IUserRepository>();
        _passwordHasherMock = Substitute.For<IPasswordHasher<User>>();
        _lockoutPolicy = new AccountLockoutPolicy();
        
        _sut = new UserService(_repositoryMock, _passwordHasherMock, _lockoutPolicy);
    }

    [Fact]
    public async Task RegisterAsync_EmailExistente_LanzaBusinessRuleException()
    {
        // Arrange
        var request = new RegisterUserRequest 
        { 
            Nombre = "Test", Apellido = "User", Email = "test@test.com", Password = "123" 
        };
        _repositoryMock.ExisteEmailAsync(request.Email, Arg.Any<CancellationToken>()).Returns(true);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<BusinessRuleException>(() => 
            _sut.RegisterAsync(request, CancellationToken.None));
            
        Assert.Equal(ErrorCodes.USR_001, exception.ErrorCode);
    }

    [Fact]
    public async Task LoginAsync_CredencialesCorrectas_RetornaLoginResponse()
    {
        // Arrange (Preparación)
        var user = new User { Id = Guid.NewGuid(), Email = "test@test.com", PasswordHash = "hash", Activo = true };
        var request = new LoginRequest { Email = "test@test.com", Password = "123" };
        
        // Simulamos que el repositorio encuentra al usuario y el hasher dice que la clave es válida
        _repositoryMock.ObtenerPorEmailAsync(request.Email, Arg.Any<CancellationToken>()).Returns(user);
        _passwordHasherMock.VerifyHashedPassword(user, user.PasswordHash, request.Password)
            .Returns(PasswordVerificationResult.Success);

        // Act (Ejecución)
        var result = await _sut.LoginAsync(request, CancellationToken.None);

        // Assert (Verificación)
        Assert.NotNull(result);
        Assert.Equal(user.Email, result.Email);
        Assert.Equal(0, user.IntentosFallidos); // Comprueba que se resetean los intentos al loguearse bien
    }

    [Fact]
    public async Task LoginAsync_PasswordIncorrecto_LanzaUsr003()
    {
        // Arrange
        var user = new User { Id = Guid.NewGuid(), Email = "test@test.com", PasswordHash = "hash", Activo = true, IntentosFallidos = 0 };
        var request = new LoginRequest { Email = "test@test.com", Password = "wrong" };
        
        // Simulamos que el hasher dice que la clave es INCORRECTA
        _repositoryMock.ObtenerPorEmailAsync(request.Email, Arg.Any<CancellationToken>()).Returns(user);
        _passwordHasherMock.VerifyHashedPassword(user, user.PasswordHash, request.Password)
            .Returns(PasswordVerificationResult.Failed);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<BusinessRuleException>(() => 
            _sut.LoginAsync(request, CancellationToken.None));
            
        // Verificamos que lance el error de credenciales incorrectas y sume 1 intento
        Assert.Equal(ErrorCodes.USR_003, exception.ErrorCode);
        Assert.Equal(1, user.IntentosFallidos); 
    }

    [Fact]
    public async Task LoginAsync_TercerIntentoFallido_BloqueaCuenta_LanzaUsr004()
    {
        // Arrange
        var user = new User 
        { 
            Id = Guid.NewGuid(), 
            Email = "test@test.com", 
            PasswordHash = "hash", 
            Activo = true, 
            IntentosFallidos = 2 // Arranca ya con 2 fallos previos
        };
        var request = new LoginRequest { Email = "test@test.com", Password = "wrong" };
        
        _repositoryMock.ObtenerPorEmailAsync(request.Email, Arg.Any<CancellationToken>()).Returns(user);
        _passwordHasherMock.VerifyHashedPassword(user, user.PasswordHash, request.Password)
            .Returns(PasswordVerificationResult.Failed);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<BusinessRuleException>(() => 
            _sut.LoginAsync(request, CancellationToken.None));
            
        // Verificamos que al fallar por tercera vez, lance USR-004 y la cuenta quede inactiva
        Assert.Equal(ErrorCodes.USR_004, exception.ErrorCode);
        Assert.False(user.Activo); 
        Assert.Equal(3, user.IntentosFallidos);
    }
}