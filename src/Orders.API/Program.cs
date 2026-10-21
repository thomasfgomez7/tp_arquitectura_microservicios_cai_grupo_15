using Orders.API.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Serilog primero, antes que cualquier otra configuración (sección 10 del enunciado).
builder.Services.AddSerilogLogging(builder.Configuration, builder.Environment);

builder.Services.AddControllers();
builder.Services.AddSwaggerDocumentation(builder.Environment);
builder.Services.AddOrderServices(builder.Configuration);
builder.Services.AddCorrelationId();
builder.Services.AddErrorHandling(builder.Configuration);
builder.Services.AddOrderHealthChecks();

var app = builder.Build();

// El orden importa: el Correlation ID tiene que existir antes del primer log, y el log de fin
// del request tiene que ver la respuesta final que armó el manejo de errores.
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<RequestLoggingMiddleware>();
app.UseExceptionHandler();

app.UseSwaggerDocumentation();
app.MapControllers();
app.MapOrderHealthChecks();

app.Run();
