namespace ParkingApp.Aplicacion.Reportes;

/// <summary>
/// Datos de un evento de ingreso o salida para mostrar en las consultas.
/// </summary>
public class EventoAccesoDto
{
    /// <summary>
    /// Placa normalizada del vehículo.
    /// </summary>
    public string Placa { get; set; } = string.Empty;

    /// <summary>
    /// Tipo de vehículo clasificado (Carro, Moto, etc.).
    /// </summary>
    public string TipoVehiculo { get; set; } = string.Empty;

    /// <summary>
    /// Tipo de evento registrado (Ingreso o Salida).
    /// </summary>
    public string TipoEvento { get; set; } = string.Empty;

    /// <summary>
    /// Fecha y hora del evento en la zona horaria de la sede.
    /// </summary>
    public DateTime FechaHora { get; set; }

    /// <summary>
    /// Número o identificador del carril donde ocurrió el evento.
    /// </summary>
    public string Carril { get; set; } = string.Empty;

    /// <summary>
    /// Indica si un operador confirmó o digitó la placa en la caja ("lectura manual").
    /// </summary>
    public bool EsLecturaManual { get; set; }

    /// <summary>
    /// Operador que validó la lectura manual, o nada si la lectura fue automática.
    /// </summary>
    public string? ValidadoPor { get; set; }
}
