namespace ParkingApp.Aplicacion.Reportes;

/// <summary>
/// Datos de un cobro generado para mostrar en las consultas.
/// </summary>
public class CobroConsultaDto
{
    /// <summary>
    /// Placa normalizada del vehículo asociado al cobro.
    /// </summary>
    public string Placa { get; set; } = string.Empty;

    /// <summary>
    /// Día operativo al que corresponde el cobro.
    /// </summary>
    public DateTime DiaOperativo { get; set; }

    /// <summary>
    /// Franja horaria aplicada (Diurna o Nocturna).
    /// </summary>
    public string Franja { get; set; } = string.Empty;

    /// <summary>
    /// Valor monetario del cobro.
    /// </summary>
    public decimal Valor { get; set; }

    /// <summary>
    /// Estado actual del cobro (Pendiente o Pagado).
    /// </summary>
    public string Estado { get; set; } = string.Empty;
}
