using Products.API.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Serilog primero, antes que cualquier otra configuración (sección 10 del enunciado).
builder.Services.AddSerilogLogging(builder.Configuration, builder.Environment);

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddProductServices();
builder.Services.AddCorrelationId();
builder.Services.AddErrorHandling(builder.Configuration);

var app = builder.Build();

// El orden importa: el Correlation ID tiene que existir antes del primer log, y el log de fin
// del request tiene que ver la respuesta final que armó el manejo de errores.
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<RequestLoggingMiddleware>();
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapControllers();

app.Run();
