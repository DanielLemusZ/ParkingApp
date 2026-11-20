namespace ParkingApp.Aplicacion.Reportes;

/// <summary>
/// Servicio de solo lectura para consultar los ingresos, salidas y cobros (F-09).
/// </summary>
public interface IServicioConsultas
{
    /// <summary>
    /// Consulta los eventos de ingreso y salida aplicando los filtros indicados.
    /// </summary>
    /// <param name="filtros">Criterios de filtrado por placa y rango de fechas.</param>
    /// <param name="cancelacion">Permite cancelar la operación.</param>
    /// <returns>Lista de eventos ordenados del más reciente al más antiguo, con la hora de la sede.</returns>
    /// <exception cref="ArgumentException">La fecha inicial es posterior a la final.</exception>
    Task<List<EventoAccesoDto>> ConsultarAccesosAsync(FiltrosConsultaDto filtros, CancellationToken cancelacion = default);

    /// <summary>
    /// Consulta los cobros aplicando los filtros indicados. El rango de fechas se aplica sobre el día operativo.
    /// </summary>
    /// <param name="filtros">Criterios de filtrado por placa, rango de fechas y estado.</param>
    /// <param name="cancelacion">Permite cancelar la operación.</param>
    /// <returns>Lista de cobros ordenados del más reciente al más antiguo.</returns>
    /// <exception cref="ArgumentException">La fecha inicial es posterior a la final.</exception>
    Task<List<CobroConsultaDto>> ConsultarCobrosAsync(FiltrosConsultaDto filtros, CancellationToken cancelacion = default);
}
