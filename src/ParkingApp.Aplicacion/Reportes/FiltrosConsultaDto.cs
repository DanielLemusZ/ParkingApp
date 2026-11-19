namespace ParkingApp.Aplicacion.Reportes;

/// <summary>
/// Filtros disponibles para consultar ingresos, salidas y cobros.
/// </summary>
public class FiltrosConsultaDto
{
    /// <summary>
    /// Placa completa o parcial del vehículo a buscar.
    /// </summary>
    public string? Placa { get; set; }

    /// <summary>
    /// Fecha inicial del rango de búsqueda.
    /// </summary>
    public DateTime? FechaInicio { get; set; }

    /// <summary>
    /// Fecha final del rango de búsqueda.
    /// </summary>
    public DateTime? FechaFin { get; set; }

    /// <summary>
    /// Estado del cobro (por ejemplo, Pendiente o Pagado).
    /// </summary>
    public string? EstadoCobro { get; set; }
}
