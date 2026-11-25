using System.Globalization;

using ParkingApp.Dominio.Accesos;
using ParkingApp.Dominio.Cobros;
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
    /// Respuesta de la caja cuando el cobro ya está pagado: solo se paga un cobro Pendiente (F-04).
    /// </summary>
    public const string CobroNoPendiente = "El cobro no está pendiente";

    /// <summary>
    /// Respuesta de la caja cuando la pasarela rechaza el pago: el cobro sigue pendiente.
    /// </summary>
    public const string PagoRechazadoPorLaPasarela = "Pago rechazado por la pasarela";

    /// <summary>
    /// Respuesta de la caja cuando la pasarela falló varias veces seguidas o no respondió al iniciar el pago.
    /// </summary>
    public const string PasarelaNoDisponible = "Pasarela no disponible: cobre en efectivo";

    /// <summary>
    /// Respuesta de la caja cuando la pasarela no respondió al consultar una transacción.
    /// </summary>
    public const string PasarelaSinRespuesta = "La pasarela no respondió. Consulte el pago de nuevo en unos segundos";

    /// <summary>
    /// Respuesta de la caja cuando el pago sigue en proceso, por ejemplo mientras se aprueba en Nequi.
    /// </summary>
    public const string PagoEnProceso = "El pago sigue en proceso en la pasarela. Consulte de nuevo en unos segundos";

    /// <summary>
    /// Respuesta de la caja cuando la transacción no la inició esta caja.
    /// </summary>
    public const string TransaccionDesconocida = "La transacción no corresponde a un pago de esta caja";

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
    /// Respuesta de la caja cuando se pagan todos los cobros de una placa, por ejemplo
    /// <c>Pago registrado: $29.000 (4 cobros)</c>. Con un solo cobro es igual a <see cref="PagoRegistrado(decimal)"/>.
    /// </summary>
    /// <param name="total">Total pagado.</param>
    /// <param name="cantidadDeCobros">Cuántos cobros se pagaron.</param>
    /// <returns>El texto que se muestra en la caja.</returns>
    public static string PagoRegistrado(decimal total, int cantidadDeCobros)
    {
        return cantidadDeCobros == 1 ? PagoRegistrado(total) : $"Pago registrado: {Pesos(total)} ({cantidadDeCobros} cobros)";
    }

    /// <summary>
    /// Respuesta de la caja cuando la placa no debe nada, por ejemplo <c>La placa ABC123 no tiene cobros pendientes</c>.
    /// </summary>
    /// <param name="placa">Placa normalizada.</param>
    /// <returns>El texto que se muestra en la caja.</returns>
    public static string SinCobrosPendientes(string placa)
    {
        return $"La placa {placa} no tiene cobros pendientes";
    }

    /// <summary>
    /// Respuesta de la caja cuando lo que debe la placa cambió desde que el operador la eligió, por ejemplo
    /// <c>No se registró el pago: el total cambió a $37.000. Revisa el detalle y vuelve a cobrar</c>.
    /// </summary>
    /// <param name="totalActual">Lo que debe ahora la placa.</param>
    /// <returns>El texto que se muestra en la caja.</returns>
    public static string TotalCambio(decimal totalActual)
    {
        return $"No se registró el pago: el total cambió a {Pesos(totalActual)}. Revisa el detalle y vuelve a cobrar";
    }

    /// <summary>
    /// Respuesta de la caja cuando la pasarela rechaza el pago, por ejemplo
    /// <c>Pago rechazado por la pasarela: fondos insuficientes</c>.
    /// </summary>
    /// <param name="motivo">Motivo que dio la pasarela, si lo dio.</param>
    /// <returns>El texto que se muestra en la caja.</returns>
    public static string PagoRechazado(string? motivo)
    {
        return string.IsNullOrWhiteSpace(motivo) ? PagoRechazadoPorLaPasarela : $"{PagoRechazadoPorLaPasarela}: {motivo.Trim()}";
    }

    /// <summary>
    /// Respuesta de la caja cuando la pasarela quedó lista, por ejemplo <c>Pago de $8.000 enviado a la pasarela</c>.
    /// </summary>
    /// <param name="total">Total que se cobra.</param>
    /// <returns>El texto que se muestra en la caja.</returns>
    public static string PagoEnLaPasarela(decimal total)
    {
        return $"Pago de {Pesos(total)} enviado a la pasarela";
    }

    /// <summary>
    /// Respuesta de la caja cuando la pasarela aprobó un valor distinto de lo pendiente, por ejemplo porque a las 06:00
    /// apareció el cobro del día nuevo mientras el conductor pagaba.
    /// </summary>
    /// <param name="pagado">Valor que aprobó la pasarela.</param>
    /// <param name="pendiente">Lo que debe ahora la placa.</param>
    /// <returns>El texto que se muestra en la caja.</returns>
    public static string ValorPagadoNoCoincide(decimal pagado, decimal pendiente)
    {
        return $"No se registró el pago: la pasarela aprobó {Pesos(pagado)} y la placa debe {Pesos(pendiente)}. Revíselo con el administrador";
    }

    /// <summary>
    /// Explica por qué existe un cobro, por ejemplo <c>Ingreso (nocturna)</c> o
    /// <c>Sigue adentro desde 2026-11-19 23:15 (diurna)</c>.
    /// </summary>
    /// <param name="motivo">Motivo del cobro.</param>
    /// <param name="franja">Franja del cobro.</param>
    /// <param name="horaDeIngreso">Hora en que entró el vehículo, en la hora de la sede.</param>
    /// <returns>El texto que se muestra en la caja.</returns>
    public static string Motivo(MotivoDeCobro motivo, FranjaTarifaria franja, DateTimeOffset horaDeIngreso)
    {
        var nombreDeFranja = franja == FranjaTarifaria.Diurna ? "diurna" : "nocturna";

        return motivo == MotivoDeCobro.SigueAdentro
            ? $"Sigue adentro desde {horaDeIngreso.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture)} ({nombreDeFranja})"
            : $"Ingreso ({nombreDeFranja})";
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
