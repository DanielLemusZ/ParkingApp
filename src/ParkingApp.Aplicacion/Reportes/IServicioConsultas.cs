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
    /// <returns>Lista de eventos ordenados del más reciente al más antiguo.</returns>
    Task<List<EventoAccesoDto>> ConsultarAccesosAsync(FiltrosConsultaDto filtros);

    /// <summary>
    /// Consulta los cobros aplicando los filtros indicados.
    /// </summary>
    /// <param name="filtros">Criterios de filtrado por placa, rango de fechas y estado.</param>
    /// <returns>Lista de cobros ordenados del más reciente al más antiguo.</returns>
    Task<List<CobroConsultaDto>> ConsultarCobrosAsync(FiltrosConsultaDto filtros);
}
