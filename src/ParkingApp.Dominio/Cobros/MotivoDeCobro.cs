namespace ParkingApp.Dominio.Cobros;

/// <summary>
/// Por qué se generó un cobro. Queda guardado para que la caja y las consultas expliquen cada cobro.
/// </summary>
public enum MotivoDeCobro
{
    /// <summary>
    /// Primer ingreso del vehículo en el día operativo (F-03).
    /// </summary>
    Ingreso,

    /// <summary>
    /// El vehículo seguía adentro cuando empezó un nuevo día operativo a las 06:00 (S-05, D-28).
    /// </summary>
    SigueAdentro,
}
