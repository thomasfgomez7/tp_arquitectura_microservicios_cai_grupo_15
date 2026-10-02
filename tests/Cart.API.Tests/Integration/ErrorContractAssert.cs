using System.Net;
using System.Text.Json;

namespace Cart.API.Tests.Integration;

/// <summary>
/// Verifica que una respuesta de error cumpla el contrato de la sección 3.1 del enunciado.
/// </summary>
public static class ErrorContractAssert
{
    public static async Task<JsonElement> IsErrorAsync(
        HttpResponseMessage response,
        HttpStatusCode status,
        string errorCode,
        string instance,
        string? errorMessage = null)
    {
        Assert.Equal(status, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var body = document.RootElement.Clone();

        Assert.StartsWith("https://tools.ietf.org/html/rfc", body.GetProperty("type").GetString());
        Assert.False(string.IsNullOrWhiteSpace(body.GetProperty("title").GetString()));
        Assert.Equal((int)status, body.GetProperty("status").GetInt32());
        Assert.False(string.IsNullOrWhiteSpace(body.GetProperty("detail").GetString()));
        Assert.Equal(instance, body.GetProperty("instance").GetString());
        Assert.Equal(errorCode, body.GetProperty("errorCode").GetString());
        Assert.False(string.IsNullOrWhiteSpace(body.GetProperty("errorMessage").GetString()));

        if (errorMessage is not null)
        {
            Assert.Equal(errorMessage, body.GetProperty("errorMessage").GetString());
        }

        // D-11: el Correlation ID viaja en el body de error y coincide con el header de la respuesta.
        var correlationId = body.GetProperty("correlationId").GetString();
        Assert.False(string.IsNullOrWhiteSpace(correlationId));
        Assert.Equal(correlationId, response.Headers.GetValues("X-Correlation-Id").Single());

        Assert.False(body.TryGetProperty("stackTrace", out _));
        Assert.False(body.TryGetProperty("exception", out _));

        return body;
    }
}
