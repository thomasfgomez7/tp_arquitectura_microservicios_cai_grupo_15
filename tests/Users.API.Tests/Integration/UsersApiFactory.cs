using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Users.API.Tests.Integration;

/// <summary>
/// Levanta Users.API en memoria para los tests de integración.
/// "LogFile:Enabled" queda listo para cuando se agregue Serilog (como en Products y Cart).
/// </summary>
public class UsersApiFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder) =>
        builder.UseSetting("LogFile:Enabled", "false");
}