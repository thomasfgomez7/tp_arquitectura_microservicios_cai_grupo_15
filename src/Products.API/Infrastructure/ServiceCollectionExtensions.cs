using Products.API.Clients;
using Products.API.Repositories;
using Products.API.Services;

namespace Products.API.Infrastructure;

/// <summary>
/// Único lugar que asocia cada interfaz con su implementación.
/// </summary>
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddProductServices(this IServiceCollection services)
    {
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<IProductRepository>(_ => new InMemoryProductRepository(ProductSeedData.Create()));
        services.AddSingleton<IOrdersClient, StubOrdersClient>();
        services.AddScoped<IProductService, ProductService>();

        return services;
    }
}
