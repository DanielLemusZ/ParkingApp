using System.Globalization;

using Microsoft.Extensions.Logging;

using ParkingApp.Aplicacion.Cobros;
using ParkingApp.Aplicacion.Configuracion;
using ParkingApp.Dominio.Accesos;
using ParkingApp.Dominio.Cobros;
using ParkingApp.Dominio.Pagos;

namespace ParkingApp.Aplicacion.Pagos;

/// <summary>
/// Pasos que comparten el pago en efectivo y el pago con la pasarela: buscar lo pendiente de una placa, pagarlo todo
/// de una vez y pedir la factura. Cada caso de uso decide el medio; esta clase solo registra.
/// </summary>
internal sealed partial class CajaDeLaPlaca
{
    private readonly IRepositorioCobros cobros;
    private readonly IRepositorioPagos pagos;
    private readonly IFacturacionElectronica facturacion;
    private readonly ISedeActual sede;
    private readonly TimeProvider reloj;
    private readonly ILogger registro;

    public CajaDeLaPlaca(
        IRepositorioCobros cobros,
        IRepositorioPagos pagos,
        IFacturacionElectronica facturacion,
        ISedeActual sede,
        TimeProvider reloj,
        ILogger registro)
    {
        this.cobros = cobros;
        this.pagos = pagos;
        this.facturacion = facturacion;
        this.sede = sede;
        this.reloj = reloj;
        this.registro = registro;
    }

    /// <summary>
    /// Busca los cobros pendientes de la placa en la sede, del día más antiguo al más reciente.
    /// </summary>
    public static async Task<(string Placa, IReadOnlyList<Cobro> Pendientes)> BuscarPendientesAsync(
        IRepositorioCobros cobros,
        ISedeActual sede,
        string? placa,
        CancellationToken cancelacion)
    {
        var placaNormalizada = Placa.Normalizar(placa);

        if (placaNormalizada.Length == 0 || placaNormalizada.Length > Placa.LargoMaximo)
        {
            return (placaNormalizada, []);
        }

        var pendientes = await cobros.ObtenerPendientesDeLaPlacaAsync(Placa.Crear(placaNormalizada), sede.Id, cancelacion);
        return (placaNormalizada, pendientes);
    }

    /// <summary>
    /// Paga todos los cobros pendientes de la placa con el medio indicado. Es idempotente: la misma referencia no
    /// registra un segundo pago.
    /// </summary>
    /// <param name="placa"></param>
    /// <param name="totalEsperado"></param>
    /// <param name="referencia"></param>
    /// <param name="operador"></param>
    /// <param name="medio"></param>
    /// <param name="transaccionDeLaPasarela"></param>
    /// <param name="motivoSiCambioElTotal">Texto cuando lo pendiente no es igual al total esperado.</param>
    /// <param name="cancelacion"></param>
    public async Task<ResultadoDePagoDeLaPlaca> PagarPendientesAsync(
        string? placa,
        decimal totalEsperado,
        Guid referencia,
        string operador,
        MedioDePago medio,
        string? transaccionDeLaPasarela,
        Func<decimal, string> motivoSiCambioElTotal,
        CancellationToken cancelacion)
    {
        // 1. La referencia va primero: en un reintento los cobros ya están pagados, y la respuesta correcta es
        // "ya estaba registrado", no "no tiene cobros pendientes".
        if (await pagos.ExisteReferenciaAsync(referencia, cancelacion))
        {
            return Rechazar(TextosDeCobro.PagoYaRegistrado);
        }

        // 2. Todos los pendientes de la placa en esta sede, del día más antiguo al más reciente.
        var (placaNormalizada, pendientes) = await BuscarPendientesAsync(cobros, sede, placa, cancelacion);

        if (pendientes.Count == 0)
        {
            return Rechazar(TextosDeCobro.SinCobrosPendientes(placaNormalizada));
        }

        // 3. Se cobra lo que el operador vio: si mientras tanto apareció un cobro (o se pagó alguno), no se paga nada.
        var total = pendientes.Sum(cobro => cobro.Valor);

        if (total != totalEsperado)
        {
            return Rechazar(motivoSiCambioElTotal(total));
        }

        // 4. Cada cobro crea su pago y pasa a Pagado. El primero lleva la referencia de la solicitud, para reconocer
        // un reintento; los demás, una nueva. Un doble clic lo frenan los índices únicos de referencia y de cobro.
        var ahora = reloj.GetUtcNow();
        var pagosNuevos = new List<Pago>();

        foreach (var cobro in pendientes)
        {
            var referenciaDelPago = pagosNuevos.Count == 0 ? referencia : Guid.CreateVersion7();
            pagosNuevos.Add(cobro.Pagar(medio, operador, referenciaDelPago, ahora, transaccionDeLaPasarela));
        }

        if (!await pagos.AgregarVariosAsync(pagosNuevos, cancelacion))
        {
            return Rechazar(TextosDeCobro.PagoYaRegistrado);
        }

        // 5. Una sola factura por el total (#24), con los dos medios. Si falla, el pago sigue registrado.
        await SolicitarFacturaAsync(placaNormalizada, pendientes, total, referencia, ahora, cancelacion);

        // 6. Se responde con la hora de la sede, que es la que ve el operador.
        var registrado = new PagoDeLaPlaca(
            placaNormalizada,
            total,
            pagosNuevos.Count,
            medio,
            pagosNuevos[0].Operador,
            referencia,
            TimeZoneInfo.ConvertTime(ahora, sede.ZonaHoraria),
            pagosNuevos[0].TransaccionDeLaPasarela);

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
