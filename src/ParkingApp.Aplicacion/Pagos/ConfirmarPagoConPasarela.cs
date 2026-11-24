using Microsoft.Extensions.Logging;

using ParkingApp.Aplicacion.Cobros;
using ParkingApp.Aplicacion.Configuracion;
using ParkingApp.Dominio.Pagos;

namespace ParkingApp.Aplicacion.Pagos;

/// <summary>
/// Caso de uso de la caja: al volver de la pasarela, consulta la transacción y, si se aprobó, paga todo lo pendiente de
/// la placa con el medio Pasarela. Es idempotente: si la misma transacción se confirma dos veces, se registra un solo
/// pago, porque la referencia de la transacción es la referencia única del pago.
/// </summary>
public sealed class ConfirmarPagoConPasarela
{
    private readonly IPasarelaDePagos pasarela;
    private readonly CajaDeLaPlaca caja;

    /// <summary>
    /// Crea el caso de uso con sus dependencias.
    /// </summary>
    /// <param name="cobros">Donde se consultan los cobros pendientes de la placa.</param>
    /// <param name="pagos">Donde se guardan los pagos.</param>
    /// <param name="pasarela">Pasarela de pagos.</param>
    /// <param name="facturacion">Proveedor de la factura electrónica (#24).</param>
    /// <param name="sede">Sede donde está la caja.</param>
    /// <param name="reloj">Da la hora del pago.</param>
    /// <param name="registro">Deja constancia si la factura falla.</param>
    public ConfirmarPagoConPasarela(
        IRepositorioCobros cobros,
        IRepositorioPagos pagos,
        IPasarelaDePagos pasarela,
        IFacturacionElectronica facturacion,
        ISedeActual sede,
        TimeProvider reloj,
        ILogger<ConfirmarPagoConPasarela> registro)
    {
        this.pasarela = pasarela;
        caja = new CajaDeLaPlaca(cobros, pagos, facturacion, sede, reloj, registro);
    }

    /// <summary>
    /// Confirma una transacción de la pasarela y, si se aprobó, registra el pago.
    /// </summary>
    /// <param name="transaccionId">Identificador de la transacción que devolvió la pasarela.</param>
    /// <param name="operador">Operador que registra el pago.</param>
    /// <param name="cancelacion">Permite cancelar la operación.</param>
    /// <returns>Si el pago se registró, si conviene consultar de nuevo, el motivo y el pago.</returns>
    public async Task<ResultadoDeConfirmacionConPasarela> EjecutarAsync(
        string? transaccionId,
        string operador,
        CancellationToken cancelacion = default)
    {
        if (string.IsNullOrWhiteSpace(transaccionId))
        {
            return Rechazar(TextosDeCobro.TransaccionDesconocida);
        }

        var consulta = await pasarela.ConsultarAsync(transaccionId.Trim(), cancelacion);

        switch (consulta.Estado)
        {
            case EstadoEnPasarela.NoDisponible:
                return new ResultadoDeConfirmacionConPasarela(false, true, TextosDeCobro.PasarelaSinRespuesta, null);
            case EstadoEnPasarela.EnProceso:
                return new ResultadoDeConfirmacionConPasarela(false, true, TextosDeCobro.PagoEnProceso, null);
            case EstadoEnPasarela.Rechazada:
                return Rechazar(TextosDeCobro.PagoRechazado(consulta.Motivo));
        }

        // La placa y la referencia viajan firmadas en la referencia de la transacción.
        if (!ReferenciaDePasarela.TryLeer(consulta.Referencia, out var placa, out var referencia))
        {
            return Rechazar(TextosDeCobro.TransaccionDesconocida);
        }

        var resultado = await caja.PagarPendientesAsync(
            placa,
            consulta.Valor,
            referencia,
            operador,
            MedioDePago.Pasarela,
            consulta.TransaccionId,
            pendiente => TextosDeCobro.ValorPagadoNoCoincide(consulta.Valor, pendiente),
            cancelacion);

        return new ResultadoDeConfirmacionConPasarela(resultado.Registrado, false, resultado.Motivo, resultado.Pago);
    }

    private static ResultadoDeConfirmacionConPasarela Rechazar(string motivo)
    {
        return new ResultadoDeConfirmacionConPasarela(false, false, motivo, null);
    }
}
