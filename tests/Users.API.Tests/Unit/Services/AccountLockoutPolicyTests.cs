using Users.API.Models;
using Users.API.Services;

namespace Users.API.Tests.Unit.Services;

public class AccountLockoutPolicyTests
{
    private readonly AccountLockoutPolicy _policy = new();

    [Theory]
    [InlineData(0, 1, true)]
    [InlineData(1, 2, true)]
    [InlineData(2, 3, false)]
    public void RegisterFailedAttempt_SumaUnIntentoYBloqueaAlLlegarAlMaximo(
        int intentosPrevios, int intentosEsperados, bool activoEsperado)
    {
        var user = new User { Activo = true, IntentosFallidos = intentosPrevios };

        _policy.RegisterFailedAttempt(user);

        Assert.Equal(intentosEsperados, user.IntentosFallidos);
        Assert.Equal(activoEsperado, user.Activo);
    }

    [Fact]
    public void ResetFailedAttempts_DejaLosIntentosEnCero()
    {
        var user = new User { Activo = true, IntentosFallidos = 2 };

        _policy.ResetFailedAttempts(user);

        Assert.Equal(0, user.IntentosFallidos);
    }

    [Theory]
    [InlineData(false, 3, true)]   // USR-004
    [InlineData(false, 0, false)]  // USR-005: bloqueo manual
    [InlineData(true, 0, false)]   // activo
    public void IsLockedOutByAttempts_DistingueElMotivoDelBloqueo(
        bool activo, int intentos, bool esperado)
    {
        var user = new User { Activo = activo, IntentosFallidos = intentos };

        Assert.Equal(esperado, _policy.IsLockedOutByAttempts(user));
    }
}