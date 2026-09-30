namespace Products.API.Infrastructure;

/// <summary>
/// Lee el Correlation ID que dejó CorrelationIdMiddleware en el request actual.
/// Es Singleton y se apoya en IHttpContextAccessor para funcionar también dentro de los
/// DelegatingHandler, que IHttpClientFactory crea en un scope propio.
/// </summary>
public class CorrelationIdAccessor(IHttpContextAccessor httpContextAccessor) : ICorrelationIdAccessor
{
    public string? CorrelationId =>
        httpContextAccessor.HttpContext?.Items[HttpContextItemKeys.CorrelationId] as string;
}
