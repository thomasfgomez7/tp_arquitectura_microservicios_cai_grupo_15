using Users.API.Models;

namespace Users.API.Services;

public class AccountLockoutPolicy
{
    private const int MaxIntentosFallidos = 3;

    public void RegistrarIntentoFallido(User user)
    {
        user.IntentosFallidos++;
        if (user.IntentosFallidos >= MaxIntentosFallidos)
        {
            user.Activo = false;
        }
    }

    public void ResetearIntentos(User user)
    {
        user.IntentosFallidos = 0;
    }
}