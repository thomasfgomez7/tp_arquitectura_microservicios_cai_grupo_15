using Users.API.Models;

namespace Users.API.Services;

/// <summary>
/// Regla de bloqueo: al acumular 3 intentos fallidos consecutivos, la cuenta se bloquea (Activo = false).
/// </summary>
public class AccountLockoutPolicy
{
    public const int MaxFailedAttempts = 3;

    public void RegisterFailedAttempt(User user)
    {
        user.IntentosFallidos++;

        if (user.IntentosFallidos >= MaxFailedAttempts)
        {
            user.Activo = false;
        }
    }

    public void ResetFailedAttempts(User user) => user.IntentosFallidos = 0;

    /// <summary>
    /// D-08: inactivo con 3 o más intentos = bloqueado por intentos (USR-004);
    /// inactivo con menos intentos = bloqueado manualmente (USR-005).
    /// </summary>
    public bool IsLockedOutByAttempts(User user) =>
        !user.Activo && user.IntentosFallidos >= MaxFailedAttempts;
}