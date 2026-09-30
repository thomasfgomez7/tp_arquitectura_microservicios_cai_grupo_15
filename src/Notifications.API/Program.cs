using Notifications.API.Clients;
using Notifications.API.Repositories;
using Notifications.API.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();

// --- INYECCIÓN DE DEPENDENCIAS (Tu código nuevo) ---
builder.Services.AddScoped<INotificationRepository, InMemoryNotificationRepository>();
builder.Services.AddScoped<IUsersClient, StubUsersClient>();
builder.Services.AddScoped<INotificationService, NotificationService>();
// ---------------------------------------------------

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapControllers();

app.Run();