using System.Net;
using System.Net.Http.Json;
using System.Text;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Notifications.API.DTOs;
using Notifications.API.Exceptions;
using Notifications.API.Services;

namespace Notifications.API.Tests.Integration;

public class NotificationsEndpointsTests(NotificationsApiFactory factory) : IClassFixture<NotificationsApiFactory>
{
    private const string SendUrl = "/api/notifications/send";

    private readonly HttpClient _client = factory.CreateClient();

    // ---------- POST /api/notifications/send ----------

    [Fact]
    public async Task Send_DatosValidos_Devuelve201ConLaNotificacionEnviada()
    {
        var response = await _client.PostAsJsonAsync(SendUrl,
            new { usuarioId = FakeUsersClient.Maria, mensaje = "Su orden #f1e2d3c4 fue confirmada.", tipo = "Email" });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var notificacion = await response.Content.ReadFromJsonAsync<NotificationResponse>();
        Assert.NotNull(notificacion);
        Assert.NotEqual(Guid.Empty, notificacion.Id);
        Assert.Equal(FakeUsersClient.Maria, notificacion.UsuarioId);
        Assert.Equal("Su orden #f1e2d3c4 fue confirmada.", notificacion.Mensaje);
        Assert.Equal("Email", notificacion.Tipo);
        Assert.Equal("Enviada", notificacion.Estado);
    }

    [Fact]
    public async Task Send_UsuarioInexistente_Devuelve404ConNtf001()
    {
        var response = await _client.PostAsJsonAsync(SendUrl,
            new { usuarioId = Guid.NewGuid(), mensaje = "Hola", tipo = "Email" });

        await ErrorContractAssert.IsErrorAsync(response, HttpStatusCode.NotFound, ErrorCodes.NTF_001,
            instance: SendUrl, errorMessage: "El usuario destinatario no fue encontrado.");
    }

    [Fact]
    public async Task Send_SinUsuarioId_Devuelve400ConNtf002()
    {
        var response = await _client.PostAsJsonAsync(SendUrl, new { mensaje = "Hola", tipo = "Email" });

        await ErrorContractAssert.IsErrorAsync(response, HttpStatusCode.BadRequest, ErrorCodes.NTF_002,
            instance: SendUrl, errorMessage: "El usuario es obligatorio.");
    }

    [Fact]
    public async Task Send_TipoNoReconocido_Devuelve400ConNtf002()
    {
        var response = await _client.PostAsJsonAsync(SendUrl,
            new { usuarioId = FakeUsersClient.Maria, mensaje = "Hola", tipo = "Fax" });

        await ErrorContractAssert.IsErrorAsync(response, HttpStatusCode.BadRequest, ErrorCodes.NTF_002,
            instance: SendUrl, errorMessage: "El tipo debe ser Email, Push o SMS.");
    }

    [Fact]
    public async Task Send_MensajeDeMasDe500Caracteres_Devuelve400ConNtf002()
    {
        var response = await _client.PostAsJsonAsync(SendUrl,
            new { usuarioId = FakeUsersClient.Maria, mensaje = new string('a', 501), tipo = "SMS" });

        await ErrorContractAssert.IsErrorAsync(response, HttpStatusCode.BadRequest, ErrorCodes.NTF_002,
            instance: SendUrl, errorMessage: "El mensaje no puede superar los 500 caracteres.");
    }

    [Fact]
    public async Task Send_BodyVacio_Devuelve400ConNtf002YListaLosProblemas()
    {
        var response = await _client.PostAsync(SendUrl, Json("{}"));

        var body = await ErrorContractAssert.IsErrorAsync(response, HttpStatusCode.BadRequest,
            ErrorCodes.NTF_002, instance: SendUrl);
        var mensaje = body.GetProperty("errorMessage").GetString()!;
        Assert.Contains("El usuario es obligatorio", mensaje);
        Assert.Contains("El mensaje es obligatorio", mensaje);
        Assert.Contains("El tipo es obligatorio", mensaje);
    }

    [Fact]
    public async Task Send_JsonInvalido_Devuelve400ConNtf002()
    {
        var response = await _client.PostAsync(SendUrl, Json("{ esto no es json"));

        await ErrorContractAssert.IsErrorAsync(response, HttpStatusCode.BadRequest, ErrorCodes.NTF_002,
            instance: SendUrl, errorMessage: "El cuerpo de la solicitud no es un JSON válido.");
    }

    [Fact]
    public async Task Send_ErrorInesperado_Devuelve500ConNtf004()
    {
        var notificationService = Substitute.For<INotificationService>();
        notificationService.SendAsync(Arg.Any<SendNotificationRequest>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("Falla simulada"));

        using var factoryConFalla = factory.WithWebHostBuilder(builder =>
            builder.ConfigureTestServices(services => services.AddScoped(_ => notificationService)));

        var response = await factoryConFalla.CreateClient().PostAsJsonAsync(SendUrl,
            new { usuarioId = FakeUsersClient.Maria, mensaje = "Hola", tipo = "Email" });

        await ErrorContractAssert.IsErrorAsync(response, HttpStatusCode.InternalServerError, ErrorCodes.NTF_004,
            instance: SendUrl, errorMessage: "Error interno al procesar la notificación.");
    }

    // ---------- GET /api/notifications/{userId} ----------

    [Fact]
    public async Task GetByUser_ConNotificaciones_Devuelve200ConLaLista()
    {
        var mensaje = $"Mensaje {Guid.NewGuid()}";
        var envio = await _client.PostAsJsonAsync(SendUrl,
            new { usuarioId = FakeUsersClient.Maria, mensaje, tipo = "Push" });
        Assert.Equal(HttpStatusCode.Created, envio.StatusCode);

        var response = await _client.GetAsync($"/api/notifications/{FakeUsersClient.Maria}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var notificaciones = await response.Content.ReadFromJsonAsync<List<NotificationResponse>>();
        Assert.Contains(notificaciones!, n => n.Mensaje == mensaje && n.Tipo == "Push");
    }

    [Fact]
    public async Task GetByUser_SinNotificaciones_Devuelve404ConNtf003()
    {
        var id = Guid.NewGuid();

        var response = await _client.GetAsync($"/api/notifications/{id}");

        await ErrorContractAssert.IsErrorAsync(response, HttpStatusCode.NotFound, ErrorCodes.NTF_003,
            instance: $"/api/notifications/{id}",
            errorMessage: "No se encontraron notificaciones para el usuario.");
    }

    [Fact]
    public async Task GetByUser_IdQueNoEsGuid_Devuelve404ConNtf003()
    {
        // D-17: antes la ruta {userId:guid} devolvía un 404 vacío, sin errorCode.
        var response = await _client.GetAsync("/api/notifications/99");

        await ErrorContractAssert.IsErrorAsync(response, HttpStatusCode.NotFound, ErrorCodes.NTF_003,
            instance: "/api/notifications/99",
            errorMessage: "No se encontraron notificaciones para el usuario.");
    }

    private static StringContent Json(string json) => new(json, Encoding.UTF8, "application/json");
}