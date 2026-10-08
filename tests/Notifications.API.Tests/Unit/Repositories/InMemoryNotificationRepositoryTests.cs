using Notifications.API.Models;
using Notifications.API.Repositories;

namespace Notifications.API.Tests.Unit.Repositories;

public class InMemoryNotificationRepositoryTests
{
    private static readonly Guid Maria = Guid.NewGuid();
    private static readonly Guid Juan = Guid.NewGuid();

    [Fact]
    public async Task GetByUserIdAsync_DevuelveSoloLasNotificacionesDelUsuario()
    {
        var repository = new InMemoryNotificationRepository();
        await repository.AddAsync(Notificacion(Maria, minuto: 1));
        await repository.AddAsync(Notificacion(Juan, minuto: 2));

        var resultado = await repository.GetByUserIdAsync(Maria);

        var unica = Assert.Single(resultado);
        Assert.Equal(Maria, unica.UsuarioId);
    }

    [Fact]
    public async Task GetByUserIdAsync_OrdenaDeLaMasViejaALaMasNueva()
    {
        var repository = new InMemoryNotificationRepository();
        var nueva = Notificacion(Maria, minuto: 30);
        var vieja = Notificacion(Maria, minuto: 5);
        await repository.AddAsync(nueva);
        await repository.AddAsync(vieja);

        var resultado = await repository.GetByUserIdAsync(Maria);

        Assert.Equal([vieja.Id, nueva.Id], resultado.Select(n => n.Id));
    }

    [Fact]
    public async Task GetByUserIdAsync_UsuarioSinNotificaciones_DevuelveListaVacia()
    {
        var repository = new InMemoryNotificationRepository();

        Assert.Empty(await repository.GetByUserIdAsync(Guid.NewGuid()));
    }

    private static Notification Notificacion(Guid usuarioId, int minuto) => new()
    {
        Id = Guid.NewGuid(),
        UsuarioId = usuarioId,
        Mensaje = "Hola",
        Tipo = NotificationType.Email,
        Estado = NotificationStatus.Enviada,
        FechaEnvio = new DateTime(2026, 1, 1, 12, minuto, 0, DateTimeKind.Utc)
    };
}