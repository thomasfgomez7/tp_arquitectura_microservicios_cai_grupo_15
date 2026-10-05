using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using Notifications.API.Clients;
using Notifications.API.DTOs;
using Notifications.API.Exceptions;
using Notifications.API.Models;
using Notifications.API.Repositories;
using Notifications.API.Services;

namespace Notifications.API.Tests.Unit;

public class NotificationServiceTests
{
    private readonly INotificationRepository _repositoryMock;
    private readonly IUsersClient _usersClientMock;
    private readonly INotificationSender _notificationSenderMock;
    private readonly NotificationService _sut;

    public NotificationServiceTests()
    {
        _repositoryMock = Substitute.For<INotificationRepository>();
        _usersClientMock = Substitute.For<IUsersClient>();
        _notificationSenderMock = Substitute.For<INotificationSender>();

        var timeProvider = new FakeTimeProvider(
            new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero));

        _sut = new NotificationService(
            _repositoryMock,
            _usersClientMock,
            _notificationSenderMock);
    }

    [Fact]
    public async Task SendAsync_UsuarioNoExiste_LanzaNtf001()
    {
        var request = new SendNotificationRequest
        {
            UsuarioId = Guid.NewGuid(),
            Mensaje = "Test",
            Tipo = "Email"
        };

        _usersClientMock
            .ExisteUsuarioAsync(request.UsuarioId, Arg.Any<CancellationToken>())
            .Returns(false);

        var exception = await Assert.ThrowsAsync<NotFoundException>(
            () => _sut.SendAsync(request, CancellationToken.None));

        Assert.Equal(ErrorCodes.NTF_001, exception.ErrorCode);
    }

    [Fact]
    public async Task SendAsync_EnvioExitoso_RegistraNotificacionEnviada()
    {
        var fechaEnvio = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        var request = new SendNotificationRequest
        {
            UsuarioId = Guid.NewGuid(),
            Mensaje = "Mensaje de prueba",
            Tipo = "Email"
        };

        _usersClientMock
            .ExisteUsuarioAsync(request.UsuarioId, Arg.Any<CancellationToken>())
            .Returns(true);

        _notificationSenderMock
            .SendAsync(Arg.Any<Notification>(), Arg.Any<CancellationToken>())
            .Returns(new NotificationSendResult("Enviada", fechaEnvio));

        var response = await _sut.SendAsync(request, CancellationToken.None);

        Assert.Equal("Enviada", response.Estado);
        Assert.Equal(fechaEnvio, response.FechaEnvio);

        await _repositoryMock.Received(1).AgregarAsync(
            Arg.Is<Notification>(notification =>
                notification.UsuarioId == request.UsuarioId
                && notification.Estado == "Enviada"
                && notification.FechaEnvio == fechaEnvio),
            Arg.Any<CancellationToken>());
    }
}