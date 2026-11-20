using System.Globalization;

using ParkingApp.Aplicacion.Cobros;
using ParkingApp.Aplicacion.Configuracion;
using ParkingApp.Dominio.Cobros;
using ParkingApp.Dominio.Pagos;

namespace ParkingApp.Aplicacion.Pagos;

/// <summary>
/// Caso de uso de la caja (F-04): registra el pago en efectivo de un cobro pendiente. El pago es idempotente:
/// la misma solicitud, con la misma referencia, registra un solo pago (RN-03). Lo usan la pantalla de la caja y la API.
/// </summary>
public sealed class RegistrarPagoEnEfectivo
{
    private readonly IRepositorioCobros cobros;
    private readonly IRepositorioPagos pagos;
    private readonly IFacturacionElectronica facturacion;
    private readonly ISedeActual sede;
    private readonly TimeProvider reloj;

    /// <summary>
    /// Crea el caso de uso con sus dependencias.
    /// </summary>
    /// <param name="cobros">Donde se busca el cobro que se paga.</param>
    /// <param name="pagos">Donde se guardan los pagos.</param>
    /// <param name="facturacion">Proveedor de la factura electrónica (D-29).</param>
    /// <param name="sede">Sede donde está la caja.</param>
    /// <param name="reloj">Reloj del sistema; en las pruebas, uno falso.</param>
    public RegistrarPagoEnEfectivo(
        IRepositorioCobros cobros,
        IRepositorioPagos pagos,
        IFacturacionElectronica facturacion,
        ISedeActual sede,
        TimeProvider reloj)
    {
        this.cobros = cobros;
        this.pagos = pagos;
        this.facturacion = facturacion;
        this.sede = sede;
        this.reloj = reloj;
    }

    /// <summary>
    /// Registra el pago en efectivo de un cobro.
    /// </summary>
    /// <param name="cobroId">Cobro que se paga.</param>
    /// <param name="referencia">
    /// Referencia única de la solicitud. La caja la genera una vez por intento de pago, para que un doble clic
    /// o un reintento manden la misma.
    /// </param>
    /// <param name="operador">Operador que recibe el efectivo.</param>
    /// <param name="cancelacion">Permite cancelar la operación.</param>
    /// <returns>Si el pago se registró, el motivo que se muestra en la caja y el pago.</returns>
    /// <exception cref="ArgumentException">La referencia está vacía o falta el operador.</exception>
    public async Task<ResultadoDePago> EjecutarAsync(
        Guid cobroId,
        Guid referencia,
        string operador,
        CancellationToken cancelacion = default)
    {
        // 1. La referencia va primero: en un reintento el cobro ya está pagado, y la respuesta correcta es
        // "ya estaba registrado", no "no está pendiente".
        if (await pagos.ExisteReferenciaAsync(referencia, cancelacion))
        {
            return Rechazar(TextosDeCobro.PagoYaRegistrado);
        }

        // 2. Solo se paga un cobro de esta sede que siga pendiente (D-27).
        var cobro = await cobros.ObtenerAsync(cobroId, cancelacion);

        if (cobro is null || cobro.SedeId != sede.Id)
        {
            return Rechazar(TextosDeCobro.CobroNoEncontrado);
        }

        if (cobro.Estado != EstadoDeCobro.Pendiente)
        {
            return Rechazar(TextosDeCobro.CobroNoPendiente);
        }

        // 3. El cobro crea su pago y pasa a Pagado; los dos se guardan juntos. Si otra solicitud con la misma
        // referencia se guardó entre la consulta y este punto, la restricción única de la base de datos la frena.
        var pago = cobro.Pagar(MedioDePago.Efectivo, operador, referencia, reloj.GetUtcNow());

        if (!await pagos.AgregarAsync(pago, cancelacion))
        {
            return Rechazar(TextosDeCobro.PagoYaRegistrado);
        }

        // 4. Se pide la factura electrónica del pago ya guardado (D-29).
        await facturacion.SolicitarFacturaAsync(pago.Valor, pago.FechaHora, Concepto(cobro), pago.Referencia.ToString(), cancelacion);

        // 5. Se responde con la hora de la sede, que es la que ve el operador.
        var registrado = new PagoRegistrado(
            pago.Id,
            cobro.Id,
            cobro.Placa.Valor,
            pago.Valor,
            pago.Medio,
            pago.Operador,
            pago.Referencia,
            TimeZoneInfo.ConvertTime(pago.FechaHora, sede.ZonaHoraria));

        return new ResultadoDePago(true, TextosDeCobro.PagoRegistrado(pago.Valor), registrado);
    }

    // Por ejemplo: "Parqueadero ABC123, día operativo 2026-11-18".
    private static string Concepto(Cobro cobro)
    {
        var dia = cobro.DiaOperativo.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        return $"Parqueadero {cobro.Placa.Valor}, día operativo {dia}";
    }

    private static ResultadoDePago Rechazar(string motivo)
    {
        return new ResultadoDePago(false, motivo, null);
    }
}
