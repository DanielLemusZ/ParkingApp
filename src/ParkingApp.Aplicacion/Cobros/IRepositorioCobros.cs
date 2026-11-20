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
    /// Guarda un cobro nuevo, salvo que la placa ya tenga uno en el mismo día operativo y sede (RN-04).
    /// </summary>
    /// <param name="cobro">Cobro pendiente.</param>
    /// <param name="cancelacion">Permite cancelar la operación.</param>
    /// <returns>
    /// <see langword="true"/> si el cobro quedó guardado; <see langword="false"/> si ya existía el cobro
    /// del día, por ejemplo porque otro ingreso simultáneo de la misma placa lo guardó primero.
    /// </returns>
    Task<bool> AgregarAsync(Cobro cobro, CancellationToken cancelacion = default);
}
