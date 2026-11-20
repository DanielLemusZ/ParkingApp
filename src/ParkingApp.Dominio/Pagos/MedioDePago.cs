namespace ParkingApp.Dominio.Pagos;

/// <summary>
/// Medio con el que se pagó un cobro. La pasarela de pagos simulada agrega su valor en su propia historia (D-29).
/// </summary>
public enum MedioDePago
{
    /// <summary>
    /// Efectivo recibido por el operador en la caja.
    /// </summary>
    Efectivo,
}
