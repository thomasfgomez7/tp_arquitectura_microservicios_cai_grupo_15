using Notifications.API.Models;

namespace Notifications.API.Services;

/// <summary>
/// Resultado de un envío: el estado (Enviada, Fallida...) y cuándo se envió.
/// </summary>
public record NotificationSendResult(NotificationStatus Estado, DateTime FechaEnvio);