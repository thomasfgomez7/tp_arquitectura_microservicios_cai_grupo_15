using System.Globalization;
using Cart.API.Models;
using Dapper;

namespace Cart.API.Repositories;

/// <summary>
/// Persistencia en SQLite con Dapper, siguiendo el ToDo de la cátedra (D-40). El carrito se guarda en dos
/// tablas: carts (una fila por usuario) y cart_items (una fila por producto). SQLite solo tiene texto, enteros
/// y reales: los Guid y la fecha se guardan como texto y se convierten acá.
/// </summary>
public class SqliteCartRepository(SqliteConnectionFactory connectionFactory) : ICartRepository
{
    public async Task<ShoppingCart?> GetByUserIdAsync(Guid usuarioId, CancellationToken cancellationToken = default)
    {
        using var connection = connectionFactory.CreateConnection();
        var parameters = new { UsuarioId = usuarioId.ToString() };

        var fechaActualizacion = await connection.QuerySingleOrDefaultAsync<string>(new CommandDefinition(
            "SELECT fecha_actualizacion FROM carts WHERE usuario_id = @UsuarioId", parameters,
            cancellationToken: cancellationToken));
        if (fechaActualizacion is null)
        {
            return null;
        }

        var items = await connection.QueryAsync<CartItemRow>(new CommandDefinition("""
            SELECT producto_id AS ProductoId, cantidad
            FROM cart_items
            WHERE usuario_id = @UsuarioId
            ORDER BY posicion
            """, parameters, cancellationToken: cancellationToken));

        return new ShoppingCart
        {
            UsuarioId = usuarioId,
            Items = items.Select(item => new CartItem
            {
                ProductoId = Guid.Parse(item.ProductoId),
                Cantidad = (int)item.Cantidad
            }).ToList(),
            FechaActualizacion = DateTime.Parse(fechaActualizacion, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind)
        };
    }

    public async Task SaveAsync(ShoppingCart cart, CancellationToken cancellationToken = default)
    {
        using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        // Todo o nada: el carrito y sus items se guardan juntos, para que nunca quede guardado a medias.
        using var transaction = connection.BeginTransaction();
        var usuarioId = cart.UsuarioId.ToString();

        await connection.ExecuteAsync(new CommandDefinition("""
            INSERT INTO carts (usuario_id, fecha_actualizacion) VALUES (@UsuarioId, @FechaActualizacion)
            ON CONFLICT (usuario_id) DO UPDATE SET fecha_actualizacion = excluded.fecha_actualizacion
            """,
            new { UsuarioId = usuarioId, FechaActualizacion = cart.FechaActualizacion.ToString("O", CultureInfo.InvariantCulture) },
            transaction, cancellationToken: cancellationToken));

        // Se reemplazan los items: así un producto quitado del carrito también desaparece de la base.
        await connection.ExecuteAsync(new CommandDefinition(
            "DELETE FROM cart_items WHERE usuario_id = @UsuarioId", new { UsuarioId = usuarioId },
            transaction, cancellationToken: cancellationToken));

        await connection.ExecuteAsync(new CommandDefinition("""
            INSERT INTO cart_items (usuario_id, producto_id, cantidad, posicion)
            VALUES (@UsuarioId, @ProductoId, @Cantidad, @Posicion)
            """,
            cart.Items.Select((item, posicion) => new
            {
                UsuarioId = usuarioId,
                ProductoId = item.ProductoId.ToString(),
                item.Cantidad,
                Posicion = posicion
            }),
            transaction, cancellationToken: cancellationToken));

        transaction.Commit();
    }

    public async Task DeleteAsync(Guid usuarioId, CancellationToken cancellationToken = default)
    {
        using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        using var transaction = connection.BeginTransaction();
        var parameters = new { UsuarioId = usuarioId.ToString() };

        await connection.ExecuteAsync(new CommandDefinition(
            "DELETE FROM cart_items WHERE usuario_id = @UsuarioId", parameters, transaction, cancellationToken: cancellationToken));
        await connection.ExecuteAsync(new CommandDefinition(
            "DELETE FROM carts WHERE usuario_id = @UsuarioId", parameters, transaction, cancellationToken: cancellationToken));

        transaction.Commit();
    }

    // Un item tal como lo devuelve SQLite.
    private sealed class CartItemRow
    {
        public string ProductoId { get; set; } = string.Empty;
        public long Cantidad { get; set; }
    }
}
