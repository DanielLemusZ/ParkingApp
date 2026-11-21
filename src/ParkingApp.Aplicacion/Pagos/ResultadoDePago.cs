using ParkingApp.Dominio.Pagos;

namespace ParkingApp.Aplicacion.Pagos;

/// <summary>
/// Respuesta de la caja al registrar un pago: si se registró, el motivo que se muestra y el pago.
/// </summary>
/// <param name="Registrado">Si el pago quedó registrado con esta solicitud.</param>
/// <param name="Motivo">Texto que se muestra en la caja.</param>
/// <param name="Pago">El pago registrado, o nada si se rechazó.</param>
public sealed record ResultadoDePago(bool Registrado, string Motivo, PagoRegistrado? Pago);

/// <summary>
/// Datos de un pago registrado, para mostrarlos en la caja.
/// </summary>
/// <param name="Id">Identificador del pago.</param>
/// <param name="CobroId">Cobro pagado.</param>
/// <param name="Placa">Placa del vehículo.</param>
/// <param name="Valor">Valor pagado en pesos colombianos.</param>
/// <param name="Medio">Medio de pago.</param>
/// <param name="Operador">Operador que registró el pago.</param>
/// <param name="Referencia">Referencia única de la solicitud de pago.</param>
/// <param name="FechaHora">Fecha y hora del pago, en la hora de la sede.</param>
public sealed record PagoRegistrado(
    Guid Id,
    Guid CobroId,
    string Placa,
    decimal Valor,
    MedioDePago Medio,
    string Operador,
    Guid Referencia,
    DateTimeOffset FechaHora);
