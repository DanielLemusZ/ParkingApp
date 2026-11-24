using ParkingApp.Aplicacion.Configuracion;
using ParkingApp.Dominio.Accesos;

namespace ParkingApp.Aplicacion.Accesos.Validaciones;

/// <summary>
/// Envía a la caja las lecturas con confianza menor que el umbral que configuró el administrador (F-06).
/// Una lectura que ya validó el operador pasa sin revisar la confianza.
/// </summary>
public sealed class ConfianzaSuficiente : ValidacionDeIngreso
{
    /// <summary>
    /// Motivo cuando la confianza es menor que el umbral. Lo muestran los dos carriles.
    /// </summary>
    public const string ConfianzaBaja = "No abre: confianza baja, hay que validar la lectura";

    private readonly IRepositorioSedes sedes;

    /// <summary>
    /// Crea el eslabón con acceso al umbral de la sede.
    /// </summary>
    /// <param name="sedes">Donde se consulta el umbral de confianza.</param>
    public ConfianzaSuficiente(IRepositorioSedes sedes)
    {
        this.sedes = sedes;
    }

    /// <inheritdoc />
    protected override async Task<ResultadoDeValidacion> RevisarAsync(SolicitudDeIngreso solicitud, CancellationToken cancelacion)
    {
        ArgumentNullException.ThrowIfNull(solicitud);

        if (solicitud.Validacion != null)
        {
            return ResultadoDeValidacion.Aceptar();
        }

        var sede = await sedes.ObtenerAsync(cancelacion);

        if (solicitud.Confianza < sede.UmbralConfianza)
        {
            return ResultadoDeValidacion.EnviarACaja(ConfianzaBaja, MotivoDeLecturaPendiente.ConfianzaBaja);
        }

        return ResultadoDeValidacion.Aceptar();
    }
}
