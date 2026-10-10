namespace Orders.API.Clients;

/// <summary>
/// Datos de un usuario que Orders lee de Users.API (GET /api/users/{id}).
/// Es un DTO propio de Orders: no se comparte código con Users (D-05).
/// </summary>
public record UserInfo
{
    public required Guid Id { get; init; }

    public required string Nombre { get; init; }

    public required string Apellido { get; init; }

    public required string Email { get; init; }

    public required bool Activo { get; init; }
}
