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
    /// Indica si la placa figura adentro de la sede: su evento más reciente es un ingreso.
    /// </summary>
    /// <param name="placa">Placa del vehículo.</param>
    /// <param name="sedeId">Sede donde se busca.</param>
    /// <param name="cancelacion">Permite cancelar la operación.</param>
    /// <returns><see langword="true"/> si tiene un ingreso sin su salida.</returns>
    Task<bool> FiguraAdentroAsync(Placa placa, Guid sedeId, CancellationToken cancelacion = default);
}
