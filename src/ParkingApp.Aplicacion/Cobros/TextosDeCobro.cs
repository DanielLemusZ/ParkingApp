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
