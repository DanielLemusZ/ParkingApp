namespace ParkingApp.Dominio.Accesos;

/// <summary>
/// Tipo de evento que registra un carril.
/// </summary>
public enum TipoDeEvento
{
    /// <summary>
    /// El vehículo entró por un carril de entrada.
    /// </summary>
    Ingreso,

    /// <summary>
    /// El vehículo salió por un carril de salida.
    /// </summary>
    Salida,
}
