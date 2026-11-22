using ParkingApp.Aplicacion.Configuracion;
using ParkingApp.Dominio.Accesos;

namespace ParkingApp.Aplicacion.Accesos;

/// <summary>
/// Caso de uso de la caja (F-06): lista las placas que el operador debe revisar. Lo usan la pantalla de la caja y la API.
/// </summary>
public sealed class ListarLecturasPendientes
{
    private readonly IRepositorioLecturasPendientes lecturas;
    private readonly ISedeActual sede;

    /// <summary>
    /// Crea el caso de uso con sus dependencias.
    /// </summary>
    /// <param name="lecturas">Donde se consultan las lecturas pendientes.</param>
    /// <param name="sede">Sede donde está la caja.</param>
    public ListarLecturasPendientes(IRepositorioLecturasPendientes lecturas, ISedeActual sede)
    {
        this.lecturas = lecturas;
        this.sede = sede;
    }

    /// <summary>
    /// Lista las lecturas pendientes, de la más antigua a la más reciente.
    /// </summary>
    /// <param name="cancelacion">Permite cancelar la operación.</param>
    /// <returns>Las lecturas, con la hora en la zona horaria de la sede.</returns>
    public async Task<IReadOnlyList<LecturaPorRevisar>> EjecutarAsync(CancellationToken cancelacion = default)
    {
        var pendientes = await lecturas.ListarPendientesAsync(sede.Id, cancelacion);
        var resultado = new List<LecturaPorRevisar>();

        foreach (var lectura in pendientes)
        {
            resultado.Add(new LecturaPorRevisar(
                lectura.Id,
                lectura.Tipo,
                lectura.PlacaLeida.Valor,
                lectura.Confianza,
                lectura.Motivo,
                lectura.Carril,
                TimeZoneInfo.ConvertTime(lectura.FechaHora, sede.ZonaHoraria)));
        }

        return resultado;
    }
}

/// <summary>
/// Lectura pendiente, como se muestra en la caja.
/// </summary>
/// <param name="Id">Identificador de la lectura.</param>
/// <param name="Tipo">Si llegó por un carril de entrada (ingreso) o de salida.</param>
/// <param name="Placa">Placa como se leyó.</param>
/// <param name="Confianza">Confianza de la lectura.</param>
/// <param name="Motivo">Por qué necesita validación.</param>
/// <param name="Carril">Número del carril.</param>
/// <param name="FechaHora">Fecha y hora de la lectura, en la hora de la sede.</param>
public sealed record LecturaPorRevisar(
    Guid Id,
    TipoDeEvento Tipo,
    string Placa,
    int Confianza,
    MotivoDeLecturaPendiente Motivo,
    int Carril,
    DateTimeOffset FechaHora);
