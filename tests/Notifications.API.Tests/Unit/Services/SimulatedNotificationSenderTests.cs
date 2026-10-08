using Microsoft.Extensions.Time.Testing;
using Notifications.API.Models;
using Notifications.API.Services;

namespace Notifications.API.Tests.Unit.Services;

public class SimulatedNotificationSenderTests
{
    [Fact]
    public async Task SendAsync_SiempreDevuelveEnviadaConLaFechaActual()
    {
        var ahora = new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);
        var sender = new SimulatedNotificationSender(new FakeTimeProvider(ahora));

        var result = await sender.SendAsync(new Notification { Id = Guid.NewGuid(), Tipo = NotificationType.SMS });

        Assert.Equal(NotificationStatus.Enviada, result.Estado);
        Assert.Equal(ahora.UtcDateTime, result.FechaEnvio);
    }
}