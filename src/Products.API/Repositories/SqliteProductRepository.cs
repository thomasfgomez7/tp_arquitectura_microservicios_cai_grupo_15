using System.Globalization;
using Dapper;
using Products.API.Models;

namespace Products.API.Repositories;

/// <summary>
/// Persistencia en SQLite con Dapper, siguiendo el ToDo de la cátedra (D-40).
/// SQLite solo tiene texto, enteros y reales: el Guid, el precio y la fecha se guardan como texto
/// y se convierten acá, así el resto del servicio sigue trabajando con sus tipos de siempre.
/// </summary>
public class SqliteProductRepository(SqliteConnectionFactory connectionFactory) : IProductRepository
{
    private const string SelectColumns = """
        SELECT id, nombre, descripcion, precio, stock, categoria, fecha_creacion AS FechaCreacion
        FROM products
        """;

    public async Task<IReadOnlyList<Product>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        using var connection = connectionFactory.CreateConnection();
        var rows = await connection.QueryAsync<ProductRow>(new CommandDefinition(
            $"{SelectColumns} ORDER BY fecha_creacion, nombre", cancellationToken: cancellationToken));

        return rows.Select(ToModel).ToList();
    }

    public async Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using var connection = connectionFactory.CreateConnection();
        var row = await connection.QuerySingleOrDefaultAsync<ProductRow>(new CommandDefinition(
            $"{SelectColumns} WHERE id = @Id", new { Id = id.ToString() }, cancellationToken: cancellationToken));

        return row is null ? null : ToModel(row);
    }

    public async Task AddAsync(Product product, CancellationToken cancellationToken = default)
    {
        using var connection = connectionFactory.CreateConnection();
        await connection.ExecuteAsync(new CommandDefinition("""
            INSERT INTO products (id, nombre, descripcion, precio, stock, categoria, fecha_creacion)
            VALUES (@Id, @Nombre, @Descripcion, @Precio, @Stock, @Categoria, @FechaCreacion)
            """, ToParameters(product), cancellationToken: cancellationToken));
    }

    public async Task UpdateAsync(Product product, CancellationToken cancellationToken = default)
    {
        using var connection = connectionFactory.CreateConnection();
        await connection.ExecuteAsync(new CommandDefinition("""
            UPDATE products
            SET nombre = @Nombre, descripcion = @Descripcion, precio = @Precio, stock = @Stock,
                categoria = @Categoria, fecha_creacion = @FechaCreacion
            WHERE id = @Id
            """, ToParameters(product), cancellationToken: cancellationToken));
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using var connection = connectionFactory.CreateConnection();
        await connection.ExecuteAsync(new CommandDefinition(
            "DELETE FROM products WHERE id = @Id", new { Id = id.ToString() }, cancellationToken: cancellationToken));
    }

    private static object ToParameters(Product product) => new
    {
        Id = product.Id.ToString(),
        product.Nombre,
        product.Descripcion,
        Precio = product.Precio.ToString(CultureInfo.InvariantCulture),
        product.Stock,
        product.Categoria,
        FechaCreacion = product.FechaCreacion.ToString("O", CultureInfo.InvariantCulture)
    };

    private static Product ToModel(ProductRow row) => new()
    {
        Id = Guid.Parse(row.Id),
        Nombre = row.Nombre,
        Descripcion = row.Descripcion,
        Precio = decimal.Parse(row.Precio, CultureInfo.InvariantCulture),
        Stock = (int)row.Stock,
        Categoria = row.Categoria,
        FechaCreacion = DateTime.Parse(row.FechaCreacion, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind)
    };

    // Una fila tal como la devuelve SQLite.
    private sealed class ProductRow
    {
        public string Id { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public string? Descripcion { get; set; }
        public string Precio { get; set; } = string.Empty;
        public long Stock { get; set; }
        public string Categoria { get; set; } = string.Empty;
        public string FechaCreacion { get; set; } = string.Empty;
    }
}
