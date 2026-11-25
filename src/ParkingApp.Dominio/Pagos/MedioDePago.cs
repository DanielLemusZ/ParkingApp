namespace ParkingApp.Dominio.Pagos;

/// <summary>
/// Medio con el que se pagó un cobro.
/// </summary>
public enum MedioDePago
{
    /// <summary>
    /// Efectivo recibido por el operador en la caja.
    /// </summary>
    Efectivo,

    /// <summary>
    /// Pasarela de pagos (tarjeta, Nequi o QR). El pago guarda la transacción que devolvió la pasarela.
    /// </summary>
    Pasarela,
}
