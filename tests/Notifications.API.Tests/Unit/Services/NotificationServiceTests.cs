using NSubstitute;
using Notifications.API.Clients;
using Notifications.API.DTOs;
using Notifications.API.Exceptions;
using Notifications.API.Models;
using Notifications.API.Repositories;
using Notifications.API.Services;

namespace Notifications.API.Tests.Unit.Services;

public class NotificationServiceTests
{
    private static readonly Guid UsuarioId = Guid.Parse("a1b2c3d4-0000-0000-0000-111122223333");
    private static readonly DateTime FechaEnvio = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

    private readonly INotificationRepository _repository = Substitute.For<INotificationRepository>();
    private readonly IUsersClient _usersClient = Substitute.For<IUsersClient>();
    private readonly INotificationSender _sender = Substitute.For<INotificationSender>();
    private readonly NotificationService _sut;

    public NotificationServiceTests()
    {
        _sut = new NotificationService(_repository, _usersClient, _sender);
    }

    // ---------- SendAsync ----------

    [Fact]
    public async Task SendAsync_EnvioExitoso_RegistraLaNotificacionConElEstadoDelSender()
    {
        _usersClient.GetUserAsync(UsuarioId, Arg.Any<CancellationToken>()).Returns(Maria());
        _sender.SendAsync(Arg.Any<Notification>(), Arg.Any<CancellationToken>())
            .Returns(new NotificationSendResult("Enviada", FechaEnvio));

        var response = await _sut.SendAsync(Request(UsuarioId));

        Assert.NotEqual(Guid.Empty, response.Id);
        Assert.Equal(UsuarioId, response.UsuarioId);
        Assert.Equal("Su orden fue confirmada.", response.Mensaje);
        Assert.Equal("Email", response.Tipo);
        Assert.Equal("Enviada", response.Estado);
        Assert.Equal(FechaEnvio, response.FechaEnvio);

        await _repository.Received(1).AgregarAsync(
            Arg.Is<Notification>(n => n.Id == response.Id
                                      && n.UsuarioId == UsuarioId
                                      && n.Estado == "Enviada"
                                      && n.FechaEnvio == FechaEnvio),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SendAsync_UsuarioNoExiste_LanzaNtf001YNoEnvia()
    {
        _usersClient.GetUserAsync(UsuarioId, Arg.Any<CancellationToken>()).Returns((UserInfo?)null);

        var exception = await Assert.ThrowsAsync<NotFoundException>(() => _sut.SendAsync(Request(UsuarioId)));

        Assert.Equal(ErrorCodes.NTF_001, exception.ErrorCode);
        Assert.Equal("El usuario destinatario no fue encontrado.", exception.Message);
        await _sender.DidNotReceive().SendAsync(Arg.Any<Notification>(), Arg.Any<CancellationToken>());
        await _repository.DidNotReceive().AgregarAsync(Arg.Any<Notification>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SendAsync_SinUsuarioId_LanzaNtf002()
    {
        var exception = await Assert.ThrowsAsync<ValidationException>(() => _sut.SendAsync(Request(null)));

        Assert.Equal(ErrorCodes.NTF_002, exception.ErrorCode);
        Assert.Equal("El usuario es obligatorio.", exception.Message);
    }

    // ---------- GetByUserIdAsync ----------

    [Fact]
    public async Task GetByUserIdAsync_ConNotificaciones_LasDevuelve()
    {
        var notificacion = new Notification
        {
            Id = Guid.NewGuid(),
            UsuarioId = UsuarioId,
            Mensaje = "Hola",
            Tipo = "Push",
            Estado = "Enviada",
            FechaEnvio = FechaEnvio
        };
        _repository.ObtenerPorUsuarioAsync(UsuarioId, Arg.Any<CancellationToken>())
            .Returns(new[] { notificacion }.AsEnumerable());

        var response = await _sut.GetByUserIdAsync(UsuarioId);

        var unica = Assert.Single(response);
        Assert.Equal(notificacion.Id, unica.Id);
        Assert.Equal("Push", unica.Tipo);
    }

    [Fact]
    public async Task GetByUserIdAsync_SinNotificaciones_LanzaNtf003()
    {
        _repository.ObtenerPorUsuarioAsync(UsuarioId, Arg.Any<CancellationToken>())
            .Returns(Enumerable.Empty<Notification>());

        var exception = await Assert.ThrowsAsync<NotFoundException>(() => _sut.GetByUserIdAsync(UsuarioId));

        Assert.Equal(ErrorCodes.NTF_003, exception.ErrorCode);
        Assert.Equal("No se encontraron notificaciones para el usuario.", exception.Message);
    }

    // ---------- Ayudas ----------

    private static SendNotificationRequest Request(Guid? usuarioId) => new()
    {
        UsuarioId = usuarioId,
        Mensaje = "Su orden fue confirmada.",
        Tipo = "Email"
    };

    private static UserInfo Maria() => new()
    {
        Id = UsuarioId,
        Nombre = "María",
        Apellido = "González",
        Email = "maria@email.com",
        Activo = true
    };
}