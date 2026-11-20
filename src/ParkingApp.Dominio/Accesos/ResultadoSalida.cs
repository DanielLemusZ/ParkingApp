namespace ParkingApp.Dominio.Accesos;

/// <summary>
/// Representa el resultado del intento de salida de un vehículo.
/// </summary>
public sealed class ResultadoSalida
{
    /// <summary>
    /// Indica si la barrera de salida debe abrirse.
    /// </summary>
    public bool AbreBarrera { get; init; }

    /// <summary>
    /// Mensaje que explica por qué la barrera abrió o se mantuvo cerrada.
    /// </summary>
    public string Mensaje { get; init; } = string.Empty;

    /// <summary>
    /// Placa normalizada del vehículo cuando la salida fue registrada.
    /// </summary>
    public string? Placa { get; init; }

    /// <summary>
    /// Tipo de vehículo clasificado al salir.
    /// </summary>
    public TipoDeVehiculo? TipoDeVehiculo { get; init; }

    /// <summary>
    /// Fecha y hora local de la sede en la que se registró la salida.
    /// </summary>
    public DateTimeOffset? FechaHora { get; init; }

    /// <summary>
    /// Identificador de la sede donde se registró la salida.
    /// </summary>
    public Guid? SedeId { get; init; }

    /// <summary>
    /// Número del carril de salida.
    /// </summary>
    public int? Carril { get; init; }

    /// <summary>
    /// Confianza de la lectura registrada.
    /// </summary>
    public int? Confianza { get; init; }
}
