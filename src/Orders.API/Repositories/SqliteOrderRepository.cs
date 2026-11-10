using System.Data;
using System.Globalization;
using Dapper;
using Orders.API.Models;

namespace Orders.API.Repositories;

/// <summary>
/// Persistencia en SQLite con Dapper, siguiendo el ToDo de la cátedra (D-40). La orden se guarda en dos
/// tablas: orders (la cabecera) y order_items (una fila por producto). SQLite solo tiene texto, enteros y
/// reales: los Guid, los importes, el estado y las fechas se guardan como texto y se convierten acá.
/// </summary>
public class SqliteOrderRepository(SqliteConnectionFactory connectionFactory) : IOrderRepository
{
    private const string SelectOrders = """
        SELECT id, usuario_id AS UsuarioId, total, estado,
               fecha_creacion AS FechaCreacion, fecha_actualizacion AS FechaActualizacion
        FROM orders
        """;

    public async Task<IReadOnlyList<Order>> GetAllAsync(
        Guid? usuarioId = null,
        Guid? productoId = null,
        CancellationToken cancellationToken = default)
    {
        using var connection = connectionFactory.CreateConnection();

        // Cada filtro es opcional: si viene null, la condición no filtra nada.
        var rows = await connection.QueryAsync<OrderRow>(new CommandDefinition($"""
            {SelectOrders}
            WHERE (@UsuarioId IS NULL OR usuario_id = @UsuarioId)
              AND (@ProductoId IS NULL OR EXISTS (
                    SELECT 1 FROM order_items WHERE order_id = orders.id AND producto_id = @ProductoId))
            ORDER BY fecha_creacion
            """,
            new { UsuarioId = usuarioId?.ToString(), ProductoId = productoId?.ToString() },
            cancellationToken: cancellationToken));

        var orders = rows.ToList();
        var itemsByOrder = await GetItemsAsync(connection, orders.Select(order => order.Id), cancellationToken);

        // Lista ya materializada y ordenada de la más vieja a la más nueva.
        return orders.Select(order => ToModel(order, itemsByOrder)).ToList();
    }

    public async Task<Order?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using var connection = connectionFactory.CreateConnection();
        var row = await connection.QuerySingleOrDefaultAsync<OrderRow>(new CommandDefinition(
            $"{SelectOrders} WHERE id = @Id", new { Id = id.ToString() }, cancellationToken: cancellationToken));
        if (row is null)
        {
            return null;
        }

        var itemsByOrder = await GetItemsAsync(connection, [row.Id], cancellationToken);
        return ToModel(row, itemsByOrder);
    }

    public async Task AddAsync(Order order, CancellationToken cancellationToken = default) =>
        await SaveAsync(order, isNew: true, cancellationToken);

    public async Task UpdateAsync(Order order, CancellationToken cancellationToken = default) =>
        await SaveAsync(order, isNew: false, cancellationToken);

    // Todo o nada: la cabecera y los items se guardan juntos, para que nunca quede una orden a medias.
    private async Task SaveAsync(Order order, bool isNew, CancellationToken cancellationToken)
    {
        using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        using var transaction = connection.BeginTransaction();

        var header = new
        {
            Id = order.Id.ToString(),
            UsuarioId = order.UsuarioId.ToString(),
            Total = order.Total.ToString(CultureInfo.InvariantCulture),
            Estado = order.Estado.ToString(),
            FechaCreacion = order.FechaCreacion.ToString("O", CultureInfo.InvariantCulture),
            FechaActualizacion = order.FechaActualizacion.ToString("O", CultureInfo.InvariantCulture)
        };

        var sql = isNew
            ? """
              INSERT INTO orders (id, usuario_id, total, estado, fecha_creacion, fecha_actualizacion)
              VALUES (@Id, @UsuarioId, @Total, @Estado, @FechaCreacion, @FechaActualizacion)
              """
            : """
              UPDATE orders
              SET usuario_id = @UsuarioId, total = @Total, estado = @Estado,
                  fecha_creacion = @FechaCreacion, fecha_actualizacion = @FechaActualizacion
              WHERE id = @Id
              """;
        await connection.ExecuteAsync(new CommandDefinition(sql, header, transaction, cancellationToken: cancellationToken));

        // Se reemplazan los items, igual que el repositorio en memoria guardaba la orden completa.
        await connection.ExecuteAsync(new CommandDefinition(
            "DELETE FROM order_items WHERE order_id = @Id", new { header.Id }, transaction, cancellationToken: cancellationToken));
        await connection.ExecuteAsync(new CommandDefinition("""
            INSERT INTO order_items (order_id, producto_id, cantidad, precio_unitario, posicion)
            VALUES (@OrderId, @ProductoId, @Cantidad, @PrecioUnitario, @Posicion)
            """,
            order.Items.Select((item, posicion) => new
            {
                OrderId = header.Id,
                ProductoId = item.ProductoId.ToString(),
                item.Cantidad,
                PrecioUnitario = item.PrecioUnitario.ToString(CultureInfo.InvariantCulture),
                Posicion = posicion
            }),
            transaction, cancellationToken: cancellationToken));

        transaction.Commit();
    }

    private static async Task<ILookup<string, OrderItemRow>> GetItemsAsync(
        IDbConnection connection,
        IEnumerable<string> orderIds,
        CancellationToken cancellationToken)
    {
        var ids = orderIds.ToList();
        if (ids.Count == 0)
        {
            return Enumerable.Empty<OrderItemRow>().ToLookup(item => item.OrderId);
        }

        // Dapper expande "IN @Ids" a la lista de ids: una sola consulta para los items de todas las órdenes.
        var items = await connection.QueryAsync<OrderItemRow>(new CommandDefinition("""
            SELECT order_id AS OrderId, producto_id AS ProductoId, cantidad, precio_unitario AS PrecioUnitario
            FROM order_items
            WHERE order_id IN @Ids
            ORDER BY posicion
            """, new { Ids = ids }, cancellationToken: cancellationToken));

        return items.ToLookup(item => item.OrderId);
    }

    private static Order ToModel(OrderRow row, ILookup<string, OrderItemRow> itemsByOrder) => new()
    {
        Id = Guid.Parse(row.Id),
        UsuarioId = Guid.Parse(row.UsuarioId),
        Items = itemsByOrder[row.Id]
            .Select(item => new OrderItem
            {
                ProductoId = Guid.Parse(item.ProductoId),
                Cantidad = (int)item.Cantidad,
                PrecioUnitario = decimal.Parse(item.PrecioUnitario, CultureInfo.InvariantCulture)
            })
            .ToList(),
        Total = decimal.Parse(row.Total, CultureInfo.InvariantCulture),
        Estado = Enum.Parse<OrderStatus>(row.Estado),
        FechaCreacion = ParseDate(row.FechaCreacion),
        FechaActualizacion = ParseDate(row.FechaActualizacion)
    };

    private static DateTime ParseDate(string value) =>
        DateTime.Parse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);

    // Filas tal como las devuelve SQLite.
    private sealed class OrderRow
    {
        public string Id { get; set; } = string.Empty;
        public string UsuarioId { get; set; } = string.Empty;
        public string Total { get; set; } = string.Empty;
        public string Estado { get; set; } = string.Empty;
        public string FechaCreacion { get; set; } = string.Empty;
        public string FechaActualizacion { get; set; } = string.Empty;
    }

    private sealed class OrderItemRow
    {
        public string OrderId { get; set; } = string.Empty;
        public string ProductoId { get; set; } = string.Empty;
        public long Cantidad { get; set; }
        public string PrecioUnitario { get; set; } = string.Empty;
    }
}
