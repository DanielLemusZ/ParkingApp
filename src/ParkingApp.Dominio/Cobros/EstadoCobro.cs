namespace ParkingApp.Dominio.Cobros;

/// <summary>
/// Estados posibles de un cobro de parqueadero.
/// </summary>
public enum EstadoCobro
{
    /// <summary>
    /// El cobro aún no ha sido pagado.
    /// </summary>
    Pendiente = 0,

    /// <summary>
    /// El cobro ya fue pagado.
    /// </summary>
    Pagado = 1
}
