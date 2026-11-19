using ParkingApp.Dominio.Accesos;
using ParkingApp.Dominio.Cobros;

namespace ParkingApp.Aplicacion.Cobros;

/// <summary>
/// Guarda y consulta los cobros de los vehículos.
/// </summary>
public interface IRepositorioCobros
{
    /// <summary>
    /// Indica si el vehículo ya tiene un cobro en el día operativo de la sede.
    /// </summary>
    /// <param name="placa">Placa del vehículo.</param>
    /// <param name="sedeId">Sede del cobro.</param>
    /// <param name="diaOperativo">Día operativo que se consulta.</param>
    /// <param name="cancelacion">Permite cancelar la operación.</param>
    /// <returns><see langword="true"/> si ya existe un cobro para esa placa, sede y día.</returns>
    Task<bool> ExisteDelDiaAsync(Placa placa, Guid sedeId, DateOnly diaOperativo, CancellationToken cancelacion = default);

    /// <summary>
    /// Guarda un cobro nuevo.
    /// </summary>
    /// <param name="cobro">Cobro pendiente.</param>
    /// <param name="cancelacion">Permite cancelar la operación.</param>
    /// <returns>Una tarea que termina cuando el cobro queda guardado.</returns>
    Task AgregarAsync(Cobro cobro, CancellationToken cancelacion = default);
}
