using ParkingApp.Dominio.Accesos;

namespace ParkingApp.Aplicacion.Accesos;

/// <summary>
/// Guarda los ingresos y las salidas de los vehículos.
/// </summary>
public interface IRepositorioEventosDeAcceso
{
    /// <summary>
    /// Guarda un evento nuevo.
    /// </summary>
    /// <param name="evento">Evento de ingreso o de salida.</param>
    /// <param name="cancelacion">Permite cancelar la operación.</param>
    /// <returns>Una tarea que termina cuando el evento queda guardado.</returns>
    Task AgregarAsync(EventoDeAcceso evento, CancellationToken cancelacion = default);

    /// <summary>
    /// Lista los vehículos que siguen adentro de la sede: su último evento es un ingreso.
    /// </summary>
    /// <param name="sedeId">Sede que se consulta.</param>
    /// <param name="cancelacion">Permite cancelar la operación.</param>
    /// <returns>Los vehículos adentro, con la hora de su último ingreso.</returns>
    Task<IReadOnlyList<VehiculoAdentro>> ListarAdentroAsync(Guid sedeId, CancellationToken cancelacion = default);
}
