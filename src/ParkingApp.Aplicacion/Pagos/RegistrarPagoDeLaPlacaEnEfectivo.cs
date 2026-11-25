using Microsoft.Extensions.Logging;

using ParkingApp.Aplicacion.Cobros;
using ParkingApp.Aplicacion.Configuracion;
using ParkingApp.Dominio.Pagos;

namespace ParkingApp.Aplicacion.Pagos;

/// <summary>
/// Caso de uso de la caja: paga en efectivo, de una vez, todo lo que debe una placa desde que entró, aunque hayan pasado
/// varios días (S-05). Como cualquier cobro pendiente bloquea la salida, se paga siempre el total.
/// Es idempotente: la misma solicitud repetida no registra un segundo pago.
/// </summary>
public sealed class RegistrarPagoDeLaPlacaEnEfectivo
{
    private readonly CajaDeLaPlaca caja;

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
        caja = new CajaDeLaPlaca(cobros, pagos, facturacion, sede, reloj, registro);
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
    public Task<ResultadoDePagoDeLaPlaca> EjecutarAsync(
        string? placa,
        decimal totalEsperado,
        Guid referencia,
        string operador,
        CancellationToken cancelacion = default)
    {
        return caja.PagarPendientesAsync(
            placa,
            totalEsperado,
            referencia,
            operador,
            MedioDePago.Efectivo,
            transaccionDeLaPasarela: null,
            TextosDeCobro.TotalCambio,
            cancelacion);
    }
}
