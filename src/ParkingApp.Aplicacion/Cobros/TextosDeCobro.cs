using System.Globalization;

using ParkingApp.Dominio.Accesos;
using ParkingApp.Dominio.Configuracion;

namespace ParkingApp.Aplicacion.Cobros;

/// <summary>
/// Textos del cobro que ven el operador y el conductor. Están en un solo lugar para que el carril,
/// la salida y la caja los muestren igual.
/// </summary>
public static class TextosDeCobro
{
    /// <summary>
    /// Motivo del carril cuando el vehículo reingresa con el cobro del día operativo pagado (RN-03).
    /// </summary>
    public const string PaseVigente = "Pase vigente";

    /// <summary>
    /// Respuesta de la caja cuando la misma solicitud de pago llega otra vez (idempotencia).
    /// </summary>
    public const string PagoYaRegistrado = "El pago ya estaba registrado";

    /// <summary>
    /// Respuesta de la caja cuando el cobro no existe en la sede.
    /// </summary>
    public const string CobroNoEncontrado = "Cobro no encontrado";

    /// <summary>
    /// Respuesta de la caja cuando el cobro ya está pagado (D-27).
    /// </summary>
    public const string CobroNoPendiente = "El cobro no está pendiente";

    // Pesos colombianos sin decimales y con punto de miles, igual en cualquier equipo.
    private static readonly NumberFormatInfo FormatoDePesos = new NumberFormatInfo
    {
        NumberGroupSeparator = ".",
        NumberDecimalSeparator = ",",
        NumberDecimalDigits = 0,
    };

    /// <summary>
    /// Escribe un valor en pesos, por ejemplo <c>$8.000</c>.
    /// </summary>
    /// <param name="valor">Valor en pesos colombianos.</param>
    /// <returns>El valor con signo de pesos y punto de miles.</returns>
    public static string Pesos(decimal valor)
    {
        return "$" + valor.ToString("N0", FormatoDePesos);
    }

    /// <summary>
    /// Motivo del carril cuando se genera un cobro, por ejemplo <c>Cobro diurno pendiente: $8.000</c>.
    /// </summary>
    /// <param name="franja">Franja del cobro.</param>
    /// <param name="valor">Valor del cobro.</param>
    /// <returns>El motivo que se muestra en el carril.</returns>
    public static string CobroPendiente(FranjaTarifaria franja, decimal valor)
    {
        var adjetivo = franja == FranjaTarifaria.Diurna ? "diurno" : "nocturno";
        return $"Cobro {adjetivo} pendiente: {Pesos(valor)}";
    }

    /// <summary>
    /// Motivo del carril cuando el vehículo ya tiene el cobro del día operativo (RN-03 y RN-04), por ejemplo
    /// <c>Reingreso sin cobro nuevo: ya existe el cobro del día operativo 2026-11-18</c>.
    /// </summary>
    /// <param name="diaOperativo">Día operativo del cobro que ya existe.</param>
    /// <returns>El motivo que se muestra en el carril.</returns>
    public static string Reingreso(DateOnly diaOperativo)
    {
        return $"Reingreso sin cobro nuevo: ya existe el cobro del día operativo {diaOperativo.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}";
    }

    /// <summary>
    /// Respuesta de la caja cuando se registra el pago, por ejemplo <c>Pago registrado: $8.000</c>.
    /// </summary>
    /// <param name="valor">Valor pagado.</param>
    /// <returns>El texto que se muestra en la caja.</returns>
    public static string PagoRegistrado(decimal valor)
    {
        return $"Pago registrado: {Pesos(valor)}";
    }

    /// <summary>
    /// Motivo del carril cuando la tarifa vale $0, por ejemplo <c>Moto: sin cobro</c>.
    /// </summary>
    /// <param name="tipoDeVehiculo">Tipo de vehículo.</param>
    /// <returns>El motivo que se muestra en el carril.</returns>
    public static string SinCobro(TipoDeVehiculo tipoDeVehiculo)
    {
        return $"{tipoDeVehiculo}: sin cobro";
    }

    /// <summary>
    /// Motivo del carril cuando no hay tarifa, por ejemplo <c>No hay tarifa vigente para carro en franja diurna</c>.
    /// </summary>
    /// <param name="tipoDeVehiculo">Tipo de vehículo.</param>
    /// <param name="franja">Franja del ingreso.</param>
    /// <returns>El motivo que se muestra en el carril.</returns>
    public static string SinTarifaVigente(TipoDeVehiculo tipoDeVehiculo, FranjaTarifaria franja)
    {
        var vehiculo = tipoDeVehiculo == TipoDeVehiculo.Moto ? "moto" : "carro";
        var nombreDeFranja = franja == FranjaTarifaria.Diurna ? "diurna" : "nocturna";
        return $"No hay tarifa vigente para {vehiculo} en franja {nombreDeFranja}";
    }
}
