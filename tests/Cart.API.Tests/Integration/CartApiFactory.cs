using Cart.API.Clients;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace Cart.API.Tests.Integration;

/// <summary>
/// Levanta Cart.API en memoria para los tests de integración: sin archivo de log y con Products.API
/// reemplazado por <see cref="FakeProductsClient"/>, para no depender de que Products esté levantado.
/// </summary>
public class CartApiFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("LogFile:Enabled", "false");
        builder.ConfigureTestServices(services => services.AddSingleton<IProductsClient, FakeProductsClient>());
    }
}

/// <summary>
/// Catálogo fijo con los mismos productos semilla de Products.API (ProductSeedData).
/// </summary>
public class FakeProductsClient : IProductsClient
{
    public static readonly Guid Notebook = Guid.Parse("3fa85f64-5717-4562-b3fc-2c963f66afa6");
    public static readonly Guid Auriculares = Guid.Parse("aaaabbbb-cccc-dddd-eeee-ffff00001111");
    public static readonly Guid Taladro = Guid.Parse("b2c3d4e5-0000-0000-0000-000000000005");

    private static readonly Dictionary<Guid, ProductInfo> Catalogo = new()
    {
        [Notebook] = new ProductInfo { Id = Notebook, Nombre = "Notebook Dell XPS 15", Precio = 1500m, Stock = 10 },
        [Auriculares] = new ProductInfo { Id = Auriculares, Nombre = "Auriculares inalámbricos", Precio = 350m, Stock = 25 },
        [Taladro] = new ProductInfo { Id = Taladro, Nombre = "Taladro percutor 750W", Precio = 120m, Stock = 2 }
    };

    public Task<ProductInfo?> GetProductAsync(Guid productId, CancellationToken cancellationToken = default) =>
        Task.FromResult(Catalogo.GetValueOrDefault(productId));
}
