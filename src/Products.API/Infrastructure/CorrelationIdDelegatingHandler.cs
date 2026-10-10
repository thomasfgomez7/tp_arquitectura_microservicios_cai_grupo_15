namespace Products.API.Infrastructure;

/// <summary>
/// Agrega el X-Correlation-Id del request actual a cada llamada HTTP saliente (sección 5.5 del enunciado):
/// así un mismo ID aparece en los logs de todos los servicios que participan del request.
/// Se engancha a los typed clients con AddHttpMessageHandler, sin tocar el código de los clientes.
/// </summary>
public class CorrelationIdDelegatingHandler(ICorrelationIdAccessor correlationIdAccessor) : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var correlationId = correlationIdAccessor.CorrelationId;

        // Sin request en curso (ej. un health check) no hay ID que propagar; y si el header
        // ya viene puesto, se respeta.
        if (correlationId is not null && !request.Headers.Contains(CorrelationIdMiddleware.HeaderName))
        {
            request.Headers.Add(CorrelationIdMiddleware.HeaderName, correlationId);
        }

        return base.SendAsync(request, cancellationToken);
    }
}
