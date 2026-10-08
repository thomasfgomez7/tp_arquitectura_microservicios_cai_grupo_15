namespace Notifications.API.Services;

/// <summary>
/// Resultado de un envío: el estado (Enviada, Fallida...) y cuándo se envió.
/// </summary>
public record NotificationSendResult(string Estado, DateTime FechaEnvio);