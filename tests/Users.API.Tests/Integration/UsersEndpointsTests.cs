using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Users.API.Exceptions;
using Users.API.Repositories;
using Users.API.Services;

namespace Users.API.Tests.Integration;

public class UsersEndpointsTests(UsersApiFactory factory) : IClassFixture<UsersApiFactory>
{
    // Usuarios de UserSeedData. María solo se usa con logins correctos, así nunca se bloquea.
    private const string MariaId = "a1b2c3d4-0000-0000-0000-111122223333";
    private const string Password = UserSeedData.DemoPassword;

    private readonly HttpClient _client = factory.CreateClient();

    // ---------- POST /api/users/register ----------

    [Fact]
    public async Task Register_DatosValidos_Devuelve201SinPasswordHash()
    {
        var email = EmailNuevo();

        var response = await _client.PostAsJsonAsync("/api/users/register",
            new { nombre = "Ana", apellido = "Martínez", email, password = Password });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await LeerJsonAsync(response);
        var id = body.GetProperty("id").GetGuid();
        Assert.Equal($"/api/users/{id}", response.Headers.Location?.AbsolutePath);
        Assert.Equal("Ana", body.GetProperty("nombre").GetString());
        Assert.Equal("Martínez", body.GetProperty("apellido").GetString());
        Assert.Equal(email, body.GetProperty("email").GetString());
        Assert.True(body.GetProperty("activo").GetBoolean());
        Assert.False(body.TryGetProperty("passwordHash", out _));
        Assert.False(body.TryGetProperty("intentosFallidos", out _));
    }

    [Fact]
    public async Task Register_EmailExistente_Devuelve409ConUsr001()
    {
        var response = await _client.PostAsJsonAsync("/api/users/register",
            new { nombre = "María", apellido = "González", email = "maria@email.com", password = Password });

        await ErrorContractAssert.IsErrorAsync(response, HttpStatusCode.Conflict, ErrorCodes.USR_001,
            instance: "/api/users/register",
            errorMessage: "El email 'maria@email.com' ya está registrado.");
    }

    [Fact]
    public async Task Register_BodyVacio_Devuelve400ConUsr002YListaLosProblemas()
    {
        var response = await _client.PostAsync("/api/users/register", Json("{}"));

        var body = await ErrorContractAssert.IsErrorAsync(response, HttpStatusCode.BadRequest,
            ErrorCodes.USR_002, instance: "/api/users/register");
        var mensaje = body.GetProperty("errorMessage").GetString()!;
        Assert.Contains("El nombre es obligatorio", mensaje);
        Assert.Contains("El apellido es obligatorio", mensaje);
        Assert.Contains("El email es obligatorio", mensaje);
        Assert.Contains("La contraseña es obligatoria", mensaje);
    }

    [Fact]
    public async Task Register_EmailVacio_Devuelve400ConUnSoloMensaje()
    {
        var response = await _client.PostAsJsonAsync("/api/users/register",
            new { nombre = "Ana", apellido = "Martínez", email = "", password = Password });

        await ErrorContractAssert.IsErrorAsync(response, HttpStatusCode.BadRequest, ErrorCodes.USR_002,
            instance: "/api/users/register", errorMessage: "El email es obligatorio.");
    }

    [Fact]
    public async Task Register_EmailMalFormado_Devuelve400ConUsr002()
    {
        var response = await _client.PostAsJsonAsync("/api/users/register",
            new { nombre = "Ana", apellido = "Martínez", email = "no-es-un-email", password = Password });

        await ErrorContractAssert.IsErrorAsync(response, HttpStatusCode.BadRequest, ErrorCodes.USR_002,
            instance: "/api/users/register", errorMessage: "El email no tiene un formato válido.");
    }

    [Fact]
    public async Task Register_JsonInvalido_Devuelve400ConUsr002()
    {
        var response = await _client.PostAsync("/api/users/register", Json("{ esto no es json"));

        await ErrorContractAssert.IsErrorAsync(response, HttpStatusCode.BadRequest, ErrorCodes.USR_002,
            instance: "/api/users/register",
            errorMessage: "El cuerpo de la solicitud no es un JSON válido.");
    }

    // ---------- POST /api/users/login ----------

    [Fact]
    public async Task Login_CredencialesCorrectas_Devuelve200ConElUsuario()
    {
        var response = await LoginAsync("maria@email.com", Password);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await LeerJsonAsync(response);
        Assert.Equal(Guid.Parse(MariaId), body.GetProperty("id").GetGuid());
        Assert.Equal("María", body.GetProperty("nombre").GetString());
        Assert.Equal("González", body.GetProperty("apellido").GetString());
        Assert.Equal("maria@email.com", body.GetProperty("email").GetString());
        Assert.False(body.TryGetProperty("passwordHash", out _));
    }

    [Fact]
    public async Task Login_PasswordIncorrecta_Devuelve401ConUsr003()
    {
        var email = await RegistrarUsuarioNuevoAsync();

        var response = await LoginAsync(email, "PasswordIncorrecta1!");

        await ErrorContractAssert.IsErrorAsync(response, HttpStatusCode.Unauthorized, ErrorCodes.USR_003,
            instance: "/api/users/login", errorMessage: "Credenciales incorrectas.");
    }

    [Fact]
    public async Task Login_EmailInexistente_Devuelve401ConUsr003()
    {
        var response = await LoginAsync(EmailNuevo(), Password);

        await ErrorContractAssert.IsErrorAsync(response, HttpStatusCode.Unauthorized, ErrorCodes.USR_003,
            instance: "/api/users/login", errorMessage: "Credenciales incorrectas.");
    }

    [Fact]
    public async Task Login_TresIntentosFallidos_ElSiguienteDevuelve403ConUsr004()
    {
        var email = await RegistrarUsuarioNuevoAsync();

        // D-09: los tres intentos fallidos responden 401; el tercero bloquea la cuenta.
        for (var intento = 1; intento <= 3; intento++)
        {
            var fallido = await LoginAsync(email, "PasswordIncorrecta1!");
            await ErrorContractAssert.IsErrorAsync(fallido, HttpStatusCode.Unauthorized, ErrorCodes.USR_003,
                instance: "/api/users/login");
        }

        // Aun con la contraseña correcta, la cuenta ya está bloqueada.
        var response = await LoginAsync(email, Password);

        await ErrorContractAssert.IsErrorAsync(response, HttpStatusCode.Forbidden, ErrorCodes.USR_004,
            instance: "/api/users/login",
            errorMessage: "Su cuenta fue bloqueada por superar el máximo de intentos fallidos. Contacte a soporte.");
    }

    [Fact]
    public async Task Login_UsuarioSemillaBloqueadoPorIntentos_Devuelve403ConUsr004()
    {
        var response = await LoginAsync("juan@email.com", Password);

        await ErrorContractAssert.IsErrorAsync(response, HttpStatusCode.Forbidden, ErrorCodes.USR_004,
            instance: "/api/users/login");
    }

    [Fact]
    public async Task Login_UsuarioBloqueadoManualmente_Devuelve403ConUsr005()
    {
        var response = await LoginAsync("carlos@email.com", Password);

        await ErrorContractAssert.IsErrorAsync(response, HttpStatusCode.Forbidden, ErrorCodes.USR_005,
            instance: "/api/users/login",
            errorMessage: "Su cuenta fue suspendida por razones de seguridad. Contacte a soporte.");
    }

    [Fact]
    public async Task Login_BodyVacio_Devuelve400ConUsr002()
    {
        var response = await _client.PostAsync("/api/users/login", Json("{}"));

        await ErrorContractAssert.IsErrorAsync(response, HttpStatusCode.BadRequest, ErrorCodes.USR_002,
            instance: "/api/users/login");
    }

    // ---------- GET /api/users/{id} ----------

    [Fact]
    public async Task GetById_UsuarioExistente_Devuelve200SinPasswordHash()
    {
        var response = await _client.GetAsync($"/api/users/{MariaId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await LeerJsonAsync(response);
        Assert.Equal(Guid.Parse(MariaId), body.GetProperty("id").GetGuid());
        Assert.Equal("maria@email.com", body.GetProperty("email").GetString());
        Assert.Equal(new DateTime(2024, 3, 10, 9, 0, 0, DateTimeKind.Utc),
            body.GetProperty("fechaRegistro").GetDateTime());
        Assert.True(body.GetProperty("activo").GetBoolean());
        Assert.False(body.TryGetProperty("passwordHash", out _));
    }

    [Fact]
    public async Task GetById_UsuarioInexistente_Devuelve404ConUsr007()
    {
        var id = Guid.NewGuid();

        var response = await _client.GetAsync($"/api/users/{id}");

        await ErrorContractAssert.IsErrorAsync(response, HttpStatusCode.NotFound, ErrorCodes.USR_007,
            instance: $"/api/users/{id}", errorMessage: "Usuario no encontrado.");
    }

    [Fact]
    public async Task GetById_IdQueNoEsGuid_Devuelve404ConUsr007()
    {
        var response = await _client.GetAsync("/api/users/99");

        await ErrorContractAssert.IsErrorAsync(response, HttpStatusCode.NotFound, ErrorCodes.USR_007,
            instance: "/api/users/99", errorMessage: "Usuario no encontrado.");
    }

    [Fact]
    public async Task GetById_ErrorInesperado_Devuelve500ConUsr006()
    {
        var userService = Substitute.For<IUserService>();
        userService.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("Falla simulada"));

        using var factoryConFalla = factory.WithWebHostBuilder(builder =>
            builder.ConfigureTestServices(services => services.AddScoped(_ => userService)));

        var response = await factoryConFalla.CreateClient().GetAsync($"/api/users/{MariaId}");

        await ErrorContractAssert.IsErrorAsync(response, HttpStatusCode.InternalServerError, ErrorCodes.USR_006,
            instance: $"/api/users/{MariaId}", errorMessage: "Error interno al procesar el usuario.");
    }

    // ---------- Ayudas ----------

    private static string EmailNuevo() => $"{Guid.NewGuid():N}@email.com";

    private async Task<string> RegistrarUsuarioNuevoAsync()
    {
        var email = EmailNuevo();
        var response = await _client.PostAsJsonAsync("/api/users/register",
            new { nombre = "Ana", apellido = "Martínez", email, password = Password });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return email;
    }

    private Task<HttpResponseMessage> LoginAsync(string email, string password) =>
        _client.PostAsJsonAsync("/api/users/login", new { email, password });

    private static StringContent Json(string json) => new(json, Encoding.UTF8, "application/json");

    private static async Task<JsonElement> LeerJsonAsync(HttpResponseMessage response)
    {
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return document.RootElement.Clone();
    }
}