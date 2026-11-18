namespace ParkingApp.Dominio.Accesos;

/// <summary>
/// Representa el resultado del intento de salida de un vehículo.
/// </summary>
public class ResultadoSalida
{
    /// <summary>
    /// Indica si la barrera de salida debe abrirse.
    /// </summary>
    public bool AbreBarrera { get; init; }

    /// <summary>
    /// Mensaje que explica por qué la barrera abrió o se mantuvo cerrada.
    /// </summary>
    public string Mensaje { get; init; } = string.Empty;
}