using System.Collections.Concurrent;
using Serilog.Core;
using Serilog.Events;

namespace Products.API.Tests.Integration;

/// <summary>
/// Sink de Serilog que guarda en memoria los eventos de log para poder verificarlos en los tests.
/// </summary>
public class CollectingSink : ILogEventSink
{
    private readonly ConcurrentQueue<LogEvent> _events = new();

    public IReadOnlyList<LogEvent> Events => _events.ToList();

    public void Emit(LogEvent logEvent) => _events.Enqueue(logEvent);

    /// <summary>
    /// Espera a que aparezca un evento que cumpla la condición. El log de fin de request se escribe
    /// cuando el middleware termina, que puede ser apenas después de que el cliente recibe la respuesta.
    /// </summary>
    public async Task<LogEvent> WaitForAsync(Func<LogEvent, bool> predicate, int timeoutMs = 2000)
    {
        var limite = DateTime.UtcNow.AddMilliseconds(timeoutMs);
        while (DateTime.UtcNow < limite)
        {
            var evento = _events.FirstOrDefault(predicate);
            if (evento is not null)
            {
                return evento;
            }
            await Task.Delay(20);
        }
        var capturados = string.Join(Environment.NewLine, _events.Select(e =>
            $"  [{e.Level}] {e.MessageTemplate.Text} | CorrelationId={e.Property("CorrelationId")}"));
        throw new Xunit.Sdk.XunitException(
            $"No se registró ningún log que cumpla la condición esperada. Logs capturados:{Environment.NewLine}{capturados}");
    }
}

public static class LogEventExtensions
{
    /// <summary>Valor de una propiedad del log como texto, sin las comillas que agrega Serilog.</summary>
    public static string? Property(this LogEvent logEvent, string name) =>
        logEvent.Properties.TryGetValue(name, out var value)
            ? value is ScalarValue { Value: var scalar } ? scalar?.ToString() : value.ToString()
            : null;
}
