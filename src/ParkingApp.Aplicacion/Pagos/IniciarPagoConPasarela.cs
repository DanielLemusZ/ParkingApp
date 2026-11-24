using ParkingApp.Aplicacion.Cobros;
using ParkingApp.Aplicacion.Configuracion;

namespace ParkingApp.Aplicacion.Pagos;

/// <summary>
/// Caso de uso de la caja: prepara el pago con la pasarela de todo lo que debe una placa y devuelve la página donde el
/// conductor paga. El pago se registra después, al confirmar la transacción (<see cref="ConfirmarPagoConPasarela"/>).
/// Si la pasarela no está disponible, la caja cobra en efectivo.
/// </summary>
public sealed class IniciarPagoConPasarela
{
    private readonly IRepositorioCobros cobros;
    private readonly IRepositorioPagos pagos;
    private readonly IPasarelaDePagos pasarela;
    private readonly ISedeActual sede;

    /// <summary>
    /// Crea el caso de uso con sus dependencias.
    /// </summary>
    /// <param name="cobros">Donde se consultan los cobros pendientes de la placa.</param>
    /// <param name="pagos">Donde se revisa si la referencia ya tiene un pago.</param>
    /// <param name="pasarela">Pasarela de pagos.</param>
    /// <param name="sede">Sede donde está la caja.</param>
    public IniciarPagoConPasarela(IRepositorioCobros cobros, IRepositorioPagos pagos, IPasarelaDePagos pasarela, ISedeActual sede)
    {
        this.cobros = cobros;
        this.pagos = pagos;
        this.pasarela = pasarela;
        this.sede = sede;
    }

    /// <summary>
    /// Prepara el pago con la pasarela de todos los cobros pendientes de la placa.
    /// </summary>
    /// <param name="placa">Placa del vehículo, sin importar mayúsculas, espacios ni guiones.</param>
    /// <param name="totalEsperado">Total que el operador le mostró al conductor.</param>
    /// <param name="referencia">Referencia única de la solicitud; se repite en los reintentos.</param>
    /// <param name="urlDeRetorno">Página de la caja a la que vuelve la pasarela.</param>
    /// <param name="cancelacion">Permite cancelar la operación.</param>
    /// <returns>La página de pago, o el motivo por el que no se puede pagar con la pasarela.</returns>
    /// <exception cref="ArgumentNullException">Falta la página de retorno.</exception>
    public async Task<ResultadoDeInicioConPasarela> EjecutarAsync(
        string? placa,
        decimal totalEsperado,
        Guid referencia,
        Uri urlDeRetorno,
        CancellationToken cancelacion = default)
    {
        ArgumentNullException.ThrowIfNull(urlDeRetorno);

        if (await pagos.ExisteReferenciaAsync(referencia, cancelacion))
        {
            return Rechazar(TextosDeCobro.PagoYaRegistrado);
        }

        var (placaNormalizada, pendientes) = await CajaDeLaPlaca.BuscarPendientesAsync(cobros, sede, placa, cancelacion);

        if (pendientes.Count == 0)
        {
            return Rechazar(TextosDeCobro.SinCobrosPendientes(placaNormalizada));
        }

        var total = pendientes.Sum(cobro => cobro.Valor);

        if (total != totalEsperado)
        {
            return Rechazar(TextosDeCobro.TotalCambio(total));
        }

        // Si la pasarela falló varias veces seguidas, ni se intenta: la caja cobra en efectivo.
        if (!pasarela.Disponible)
        {
            return Rechazar(TextosDeCobro.PasarelaNoDisponible);
        }

        var solicitud = new SolicitudDePagoEnPasarela(
            ReferenciaDePasarela.Escribir(placaNormalizada, referencia),
            total,
            $"Parqueadero {placaNormalizada}",
            urlDeRetorno);
        var inicio = await pasarela.IniciarAsync(solicitud, cancelacion);

        if (!inicio.Disponible || inicio.UrlDePago is null)
        {
            return Rechazar(TextosDeCobro.PasarelaNoDisponible);
        }

        return new ResultadoDeInicioConPasarela(true, TextosDeCobro.PagoEnLaPasarela(total), inicio.UrlDePago);
    }

    private static ResultadoDeInicioConPasarela Rechazar(string motivo)
    {
        return new ResultadoDeInicioConPasarela(false, motivo, null);
    }
}
