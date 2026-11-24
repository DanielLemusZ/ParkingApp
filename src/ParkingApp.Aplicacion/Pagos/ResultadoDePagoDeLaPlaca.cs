using ParkingApp.Dominio.Pagos;

namespace ParkingApp.Aplicacion.Pagos;

/// <summary>
/// Resultado de pagar todo lo pendiente de una placa en la caja.
/// </summary>
/// <param name="Registrado">Si el pago quedó registrado.</param>
/// <param name="Motivo">Texto para el operador, por ejemplo <c>Pago registrado: $29.000 (4 cobros)</c>.</param>
/// <param name="Pago">El pago, o <see langword="null"/> si no se registró.</param>
public sealed record ResultadoDePagoDeLaPlaca(bool Registrado, string Motivo, PagoDeLaPlaca? Pago);

/// <summary>
/// Pago de todos los cobros pendientes de una placa, como lo ve el operador.
/// </summary>
/// <param name="Placa">Placa del vehículo.</param>
/// <param name="Total">Suma de los cobros pagados, en pesos colombianos.</param>
/// <param name="Cobros">Cuántos cobros se pagaron: uno por cada día operativo pendiente.</param>
/// <param name="Medio">Medio de pago.</param>
/// <param name="Operador">Operador que registró el pago.</param>
/// <param name="Referencia">Referencia única de la solicitud.</param>
/// <param name="FechaHora">Fecha y hora del pago, en la hora de la sede.</param>
/// <param name="TransaccionDeLaPasarela">Transacción de la pasarela, o <see langword="null"/> si se pagó en efectivo.</param>
public sealed record PagoDeLaPlaca(
    string Placa,
    decimal Total,
    int Cobros,
    MedioDePago Medio,
    string Operador,
    Guid Referencia,
    DateTimeOffset FechaHora,
    string? TransaccionDeLaPasarela = null);
