namespace ParkingApp.Dominio.Accesos;

/// <summary>
/// Tipo de vehículo identificado a partir del formato de su placa.
/// </summary>
public enum TipoDeVehiculo
{
    /// <summary>
    /// Automóvil con placa de tres letras y tres dígitos.
    /// </summary>
    Carro,

    /// <summary>
    /// Motocicleta con placa de tres letras, dos dígitos y una letra.
    /// </summary>
    Moto,

    /// <summary>
    /// Evento histórico cuya placa no coincide con los formatos actuales.
    /// </summary>
    NoClasificado,
}
