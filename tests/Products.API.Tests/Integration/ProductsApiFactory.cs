using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Products.API.Tests.Integration;

/// <summary>
/// Levanta Products.API en memoria para los tests de integración, sin escribir el archivo de log.
/// </summary>
public class ProductsApiFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder) =>
        builder.UseSetting("LogFile:Enabled", "false");
}
