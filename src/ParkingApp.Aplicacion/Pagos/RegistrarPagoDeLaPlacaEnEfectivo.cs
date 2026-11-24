using System.Globalization;

using Microsoft.Extensions.Logging;

using ParkingApp.Aplicacion.Cobros;
using ParkingApp.Aplicacion.Configuracion;
using ParkingApp.Dominio.Accesos;
using ParkingApp.Dominio.Cobros;
using ParkingApp.Dominio.Pagos;

namespace ParkingApp.Aplicacion.Pagos;

/// <summary>
/// Caso de uso de la caja: paga en efectivo, de una vez, todo lo que debe una placa desde que entró, aunque hayan pasado
/// varios días (S-05). Como cualquier cobro pendiente bloquea la salida, se paga siempre el total.
/// Es idempotente: la misma solicitud repetida no registra un segundo pago.
/// </summary>
public sealed partial class RegistrarPagoDeLaPlacaEnEfectivo
{
    private readonly IRepositorioCobros cobros;
    private readonly IRepositorioPagos pagos;
    private readonly IFacturacionElectronica facturacion;
    private readonly ISedeActual sede;
    private readonly TimeProvider reloj;
    private readonly ILogger<RegistrarPagoDeLaPlacaEnEfectivo> registro;

    /// <summary>
    /// Crea el caso de uso con sus dependencias.
    /// </summary>
    /// <param name="cobros">Donde se consultan los cobros pendientes de la placa.</param>
    /// <param name="pagos">Donde se guardan los pagos.</param>
    /// <param name="facturacion">Proveedor de la factura electrónica (#24).</param>
    /// <param name="sede">Sede donde está la caja.</param>
    /// <param name="reloj">Da la hora del pago.</param>
    /// <param name="registro">Deja constancia si la factura falla.</param>
    public RegistrarPagoDeLaPlacaEnEfectivo(
        IRepositorioCobros cobros,
        IRepositorioPagos pagos,
        IFacturacionElectronica facturacion,
        ISedeActual sede,
        TimeProvider reloj,
        ILogger<RegistrarPagoDeLaPlacaEnEfectivo> registro)
    {
        this.cobros = cobros;
        this.pagos = pagos;
        this.facturacion = facturacion;
        this.sede = sede;
        this.reloj = reloj;
        this.registro = registro;
    }

    /// <summary>
    /// Paga en efectivo todos los cobros pendientes de la placa, de cualquier día operativo. Cada cobro queda con su
    /// propio pago, y todos se guardan juntos: o se pagan todos o ninguno.
    /// </summary>
    /// <param name="placa">Placa del vehículo, sin importar mayúsculas, espacios ni guiones.</param>
    /// <param name="totalEsperado">
    /// Total que el operador vio y recibió. Si los pendientes cambiaron desde entonces (por ejemplo, a las 06:00 se
    /// generó el cobro del día nuevo), no se paga nada y se avisa el total nuevo.
    /// </param>
    /// <param name="referencia">Referencia única de la solicitud; se repite en los reintentos.</param>
    /// <param name="operador">Operador que registra el pago.</param>
    /// <param name="cancelacion">Permite cancelar la operación.</param>
    /// <returns>Si el pago se registró, el motivo y el pago con el total.</returns>
    public async Task<ResultadoDePagoDeLaPlaca> EjecutarAsync(
        string? placa,
        decimal totalEsperado,
        Guid referencia,
        string operador,
        CancellationToken cancelacion = default)
    {
        // 1. La referencia va primero: en un reintento los cobros ya están pagados, y la respuesta correcta es
        // "ya estaba registrado", no "no tiene cobros pendientes".
        if (await pagos.ExisteReferenciaAsync(referencia, cancelacion))
        {
            return Rechazar(TextosDeCobro.PagoYaRegistrado);
        }

        var placaNormalizada = Placa.Normalizar(placa);

        if (placaNormalizada.Length == 0 || placaNormalizada.Length > Placa.LargoMaximo)
        {
            return Rechazar(TextosDeCobro.SinCobrosPendientes(placaNormalizada));
        }

        // 2. Todos los pendientes de la placa en esta sede, del día más antiguo al más reciente.
        var pendientes = await cobros.ObtenerPendientesDeLaPlacaAsync(Placa.Crear(placaNormalizada), sede.Id, cancelacion);

        if (pendientes.Count == 0)
        {
            return Rechazar(TextosDeCobro.SinCobrosPendientes(placaNormalizada));
        }

        // 3. Se cobra lo que el operador vio: si mientras tanto apareció un cobro (o se pagó alguno), no se paga nada.
        var total = pendientes.Sum(cobro => cobro.Valor);

        if (total != totalEsperado)
        {
            return Rechazar(TextosDeCobro.TotalCambio(total));
        }

        // 4. Cada cobro crea su pago y pasa a Pagado. El primero lleva la referencia de la solicitud, para reconocer
        // un reintento; los demás, una nueva. Un doble clic lo frenan los índices únicos de referencia y de cobro.
        var ahora = reloj.GetUtcNow();
        var pagosNuevos = new List<Pago>();

        foreach (var cobro in pendientes)
        {
            var referenciaDelPago = pagosNuevos.Count == 0 ? referencia : Guid.CreateVersion7();
            pagosNuevos.Add(cobro.Pagar(MedioDePago.Efectivo, operador, referenciaDelPago, ahora));
        }

        if (!await pagos.AgregarVariosAsync(pagosNuevos, cancelacion))
        {
            return Rechazar(TextosDeCobro.PagoYaRegistrado);
        }

        // 5. Una sola factura por el total (#24). Si falla, el pago sigue registrado.
        await SolicitarFacturaAsync(placaNormalizada, pendientes, total, referencia, ahora, cancelacion);

        // 6. Se responde con la hora de la sede, que es la que ve el operador.
        var registrado = new PagoDeLaPlaca(
            placaNormalizada,
            total,
            pagosNuevos.Count,
            MedioDePago.Efectivo,
            pagosNuevos[0].Operador,
            referencia,
            TimeZoneInfo.ConvertTime(ahora, sede.ZonaHoraria));

        return new ResultadoDePagoDeLaPlaca(true, TextosDeCobro.PagoRegistrado(total, pagosNuevos.Count), registrado);
    }

    private static ResultadoDePagoDeLaPlaca Rechazar(string motivo)
    {
        return new ResultadoDePagoDeLaPlaca(false, motivo, null);
    }

    // Por ejemplo: "Parqueadero SGA521, días operativos 2026-11-19 a 2026-11-22".
    private static string Concepto(string placa, IReadOnlyList<Cobro> cobrosPagados)
    {
        var primero = cobrosPagados[0].DiaOperativo.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        var ultimo = cobrosPagados[^1].DiaOperativo.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

        return primero == ultimo
            ? $"Parqueadero {placa}, día operativo {primero}"
            : $"Parqueadero {placa}, días operativos {primero} a {ultimo}";
    }

    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "No se pudo solicitar la factura electrónica del pago {Referencia} (placa {Placa}). El pago quedó registrado.")]
    private static partial void FacturaNoSolicitada(ILogger registro, Exception error, Guid referencia, string placa);

    private async Task SolicitarFacturaAsync(
        string placa,
        IReadOnlyList<Cobro> cobrosPagados,
        decimal total,
        Guid referencia,
        DateTimeOffset fechaHora,
        CancellationToken cancelacion)
    {
        try
        {
            await facturacion.SolicitarFacturaAsync(total, fechaHora, Concepto(placa, cobrosPagados), referencia.ToString(), cancelacion);
        }
#pragma warning disable CA1031 // Se captura cualquier excepción a propósito: ninguna falla del proveedor de facturas
        // debe tumbar un pago que ya quedó guardado.
        catch (Exception error)
#pragma warning restore CA1031
        {
            FacturaNoSolicitada(registro, error, referencia, placa);
        }
    }
}
