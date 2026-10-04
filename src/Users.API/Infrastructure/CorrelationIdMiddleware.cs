namespace Users.API.Infrastructure;

public class CorrelationIdMiddleware(RequestDelegate next)
{
    public const string HeaderName = "X-Correlation-Id";

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = GetValidHeader(context)
            ?? Guid.NewGuid().ToString();

        context.Items[HttpContextItemKeys.CorrelationId] = correlationId;

        context.Response.OnStarting(() =>
        {
            context.Response.Headers[HeaderName] = correlationId;
            return Task.CompletedTask;
        });

        await next(context);
    }

    private static string? GetValidHeader(HttpContext context)
    {
        var value = context.Request.Headers[HeaderName].ToString();

        if (value.Length is < 1 or > 64)
        {
            return null;
        }

        var isValid = value.All(character =>
            char.IsAsciiLetterOrDigit(character)
            || character is '.' or '_' or '-');

        return isValid ? value : null;
    }
}