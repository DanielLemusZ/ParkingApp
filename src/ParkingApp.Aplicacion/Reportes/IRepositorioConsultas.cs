using ParkingApp.Dominio.Accesos;
using ParkingApp.Dominio.Cobros;

namespace ParkingApp.Aplicacion.Reportes;

/// <summary>
/// Puerto de solo lectura para las consultas del administrador.
/// </summary>
public interface IRepositorioConsultas
{
    /// <summary>
    /// Consulta los ingresos y las salidas, del más reciente al más antiguo. Los filtros nulos no se aplican.
    /// </summary>
    /// <param name="fechaInicioUtc">Desde cuándo, incluido.</param>
    /// <param name="fechaFinUtc">Hasta cuándo, sin incluirlo.</param>
    /// <param name="placaNormalizada">Placa completa o parcial, ya normalizada.</param>
    /// <param name="cancelacion">Permite cancelar la operación.</param>
    /// <returns>Los eventos que cumplen los filtros.</returns>
    Task<List<EventoDeAcceso>> ConsultarAccesosAsync(DateTimeOffset? fechaInicioUtc, DateTimeOffset? fechaFinUtc, string? placaNormalizada, CancellationToken cancelacion = default);

    /// <summary>
    /// Consulta los cobros, del más reciente al más antiguo. Los filtros nulos no se aplican.
    /// </summary>
    /// <param name="diaInicio">Primer día operativo, incluido.</param>
    /// <param name="diaFin">Último día operativo, incluido.</param>
    /// <param name="placaNormalizada">Placa completa o parcial, ya normalizada.</param>
    /// <param name="estado">Estado del cobro.</param>
    /// <param name="cancelacion">Permite cancelar la operación.</param>
    /// <returns>Los cobros que cumplen los filtros.</returns>
    Task<List<Cobro>> ConsultarCobrosAsync(DateOnly? diaInicio, DateOnly? diaFin, string? placaNormalizada, EstadoDeCobro? estado, CancellationToken cancelacion = default);
}
