using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Users.API.Tests.Integration;

/// <summary>
/// Levanta Users.API en memoria para los tests de integración, sin escribir el archivo de log.
/// </summary>
public class UsersApiFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder) =>
        builder.UseSetting("LogFile:Enabled", "false");
}