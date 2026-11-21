using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

using ParkingApp.Infraestructura.Identidad;

namespace ParkingApp.Web.Cuenta;

/// <summary>
/// Revisa cada minuto que la sesión de una pantalla interactiva siga valiendo. Una pantalla interactiva no hace
/// solicitudes HTTP nuevas (usa una conexión SignalR abierta), así que la cookie no se vuelve a revisar sola. Si el
/// administrador desactivó al usuario, le cambió el rol o le restableció la contraseña, el sello de seguridad cambió:
/// la sesión deja de valer y la pantalla lleva al inicio de sesión (criterio 6 del #68).
/// </summary>
internal sealed class SesionRevalidada : RevalidatingServerAuthenticationStateProvider
{
    private readonly IServiceScopeFactory fabricaDeAlcances;
    private readonly IOptions<IdentityOptions> opcionesDeIdentidad;

    public SesionRevalidada(
        ILoggerFactory registros,
        IServiceScopeFactory fabricaDeAlcances,
        IOptions<IdentityOptions> opcionesDeIdentidad)
        : base(registros)
    {
        this.fabricaDeAlcances = fabricaDeAlcances;
        this.opcionesDeIdentidad = opcionesDeIdentidad;
    }

    protected override TimeSpan RevalidationInterval => TimeSpan.FromMinutes(1);

    protected override async Task<bool> ValidateAuthenticationStateAsync(AuthenticationState authenticationState, CancellationToken cancellationToken)
    {
        // Este proveedor vive mientras la pantalla está abierta; la base de datos se consulta en un alcance propio.
        await using var alcance = fabricaDeAlcances.CreateAsyncScope();
        var usuarios = alcance.ServiceProvider.GetRequiredService<UserManager<Usuario>>();

        // Si el usuario fue borrado, la sesión ya no vale.
        var usuario = await usuarios.GetUserAsync(authenticationState.User);

        if (usuario is null)
        {
            return false;
        }

        if (!usuarios.SupportsUserSecurityStamp)
        {
            return true;
        }

        var tipoDelSello = opcionesDeIdentidad.Value.ClaimsIdentity.SecurityStampClaimType;
        var selloDeLaSesion = authenticationState.User.FindFirst(tipoDelSello)?.Value;
        var selloActual = await usuarios.GetSecurityStampAsync(usuario);

        return selloDeLaSesion == selloActual;
    }
}
