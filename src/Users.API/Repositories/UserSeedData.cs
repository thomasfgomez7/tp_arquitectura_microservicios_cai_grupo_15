using Microsoft.AspNetCore.Identity;
using Users.API.Models;

namespace Users.API.Repositories;

/// <summary>
/// Usuarios precargados para la demo. Los IDs son fijos para poder usarlos desde Orders y Notifications;
/// el primero es el del ejemplo del enunciado. Todos tienen la contraseña <see cref="DemoPassword"/>.
/// </summary>
public static class UserSeedData
{
    public const string DemoPassword = "MiPassword123!";

    public static IReadOnlyList<User> Create(IPasswordHasher<User> passwordHasher)
    {
        User[] users =
        [
            new()
            {
                Id = Guid.Parse("a1b2c3d4-0000-0000-0000-111122223333"),
                Nombre = "María",
                Apellido = "González",
                Email = "maria@email.com",
                FechaRegistro = new DateTime(2024, 3, 10, 9, 0, 0, DateTimeKind.Utc),
                Activo = true,
                IntentosFallidos = 0
            },
            new()
            {
                // Bloqueado por intentos fallidos: para mostrar USR-004 (D-08).
                Id = Guid.Parse("a1b2c3d4-0000-0000-0000-000000000002"),
                Nombre = "Juan",
                Apellido = "Pérez",
                Email = "juan@email.com",
                FechaRegistro = new DateTime(2024, 3, 11, 10, 0, 0, DateTimeKind.Utc),
                Activo = false,
                IntentosFallidos = 3
            },
            new()
            {
                // Bloqueado manualmente (fraude): para mostrar USR-005 (D-08).
                Id = Guid.Parse("a1b2c3d4-0000-0000-0000-000000000003"),
                Nombre = "Carlos",
                Apellido = "López",
                Email = "carlos@email.com",
                FechaRegistro = new DateTime(2024, 3, 12, 11, 0, 0, DateTimeKind.Utc),
                Activo = false,
                IntentosFallidos = 0
            }
        ];

        foreach (var user in users)
        {
            user.PasswordHash = passwordHasher.HashPassword(user, DemoPassword);
        }

        return users;
    }
}