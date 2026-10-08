using System.Net;

namespace Notifications.API.Clients;

/// <summary>
/// Cliente HTTP tipado de Users.API. El HttpClient lo crea IHttpClientFactory con la URL base
/// configurada en "Services:UsersApi:BaseUrl".
/// </summary>
public class UsersClient(HttpClient httpClient) : IUsersClient
{
    public async Task<UserInfo?> GetUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.GetAsync($"api/users/{userId}", cancellationToken);

        // Se revisa el status ANTES de leer el body (sección 10 del enunciado): un 404 trae el JSON
        // de error de Users (USR-007), no un usuario.
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        // Cualquier otro error es una falla de Users, no un dato del negocio:
        // se lanza la excepción y termina en un 500 con NTF-004.
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<UserInfo>(cancellationToken);
    }
}