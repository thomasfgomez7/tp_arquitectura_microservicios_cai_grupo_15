using NSubstitute;
using Notifications.API.Clients;
using Notifications.API.DTOs;
using Notifications.API.Exceptions;
using Notifications.API.Repositories;
using Notifications.API.Services;

namespace Notifications.API.Tests.Unit;

public class NotificationServiceTests
{
    private readonly INotificationRepository _repositoryMock;
    private readonly IUsersClient _usersClientMock;
    private readonly NotificationService _sut;

    public NotificationServiceTests()
    {
        _repositoryMock = Substitute.For<INotificationRepository>();
        _usersClientMock = Substitute.For<IUsersClient>();
        _sut = new NotificationService(_repositoryMock, _usersClientMock);
    }

    [Fact]
    public async Task SendAsync_UsuarioNoExiste_LanzaNtf001()
    {
        // Arrange
        var request = new SendNotificationRequest { UsuarioId = Guid.NewGuid(), Mensaje = "Test", Tipo = "Email" };
        _usersClientMock.ExisteUsuarioAsync(request.UsuarioId, Arg.Any<CancellationToken>()).Returns(false);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<NotFoundException>(() => 
            _sut.SendAsync(request, CancellationToken.None));
            
        Assert.Equal(ErrorCodes.NTF_001, exception.ErrorCode);
    }
}