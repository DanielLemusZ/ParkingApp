namespace ParkingApp.Dominio.Cobros;

/// <summary>
/// Estado de un cobro. El pase vigente no se guarda: es un cobro pagado del día operativo actual.
/// </summary>
public enum EstadoDeCobro
{
    /// <summary>
    /// Generado al primer ingreso del día y aún sin pagar. Impide la salida.
    /// </summary>
    Pendiente,

    /// <summary>
    /// Pagado. Deja salir y reingresar sin cobro nuevo hasta el fin del día operativo.
    /// </summary>
    Pagado,
}
