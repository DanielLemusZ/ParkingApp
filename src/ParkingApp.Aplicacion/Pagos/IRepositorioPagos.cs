using ParkingApp.Dominio.Pagos;

namespace ParkingApp.Aplicacion.Pagos;

/// <summary>
/// Guarda y consulta los pagos de los cobros.
/// </summary>
public interface IRepositorioPagos
{
    /// <summary>
    /// Indica si ya hay un pago registrado con la referencia.
    /// </summary>
    /// <param name="referencia">Referencia única de la solicitud de pago.</param>
    /// <param name="cancelacion">Permite cancelar la operación.</param>
    /// <returns><see langword="true"/> si la referencia ya tiene un pago.</returns>
    Task<bool> ExisteReferenciaAsync(Guid referencia, CancellationToken cancelacion = default);

    /// <summary>
    /// Guarda el pago y, en la misma transacción, el cobro que pasó a Pagado. El cobro debe haberse leído con
    /// <see cref="Cobros.IRepositorioCobros.ObtenerAsync"/> en la misma operación.
    /// </summary>
    /// <param name="pago">Pago creado por <c>Cobro.Pagar</c>.</param>
    /// <param name="cancelacion">Permite cancelar la operación.</param>
    /// <returns>
    /// <see langword="true"/> si el pago quedó guardado; <see langword="false"/> si la referencia o el cobro ya tenían
    /// un pago, por ejemplo porque un doble clic envió la misma solicitud dos veces. En ese caso no se guarda nada.
    /// </returns>
    Task<bool> AgregarAsync(Pago pago, CancellationToken cancelacion = default);

    /// <summary>
    /// Guarda varios pagos y, en la misma transacción, sus cobros que pasaron a Pagado: se guardan todos o ninguno.
    /// Los cobros deben haberse leído con <see cref="Cobros.IRepositorioCobros.ObtenerPendientesDeLaPlacaAsync"/> en la
    /// misma operación.
    /// </summary>
    /// <param name="pagos">Pagos creados por <c>Cobro.Pagar</c>, uno por cobro.</param>
    /// <param name="cancelacion">Permite cancelar la operación.</param>
    /// <returns>
    /// <see langword="true"/> si todos quedaron guardados; <see langword="false"/> si alguna referencia o algún cobro ya
    /// tenía un pago, por ejemplo por un doble clic. En ese caso no se guarda ninguno.
    /// </returns>
    Task<bool> AgregarVariosAsync(IReadOnlyList<Pago> pagos, CancellationToken cancelacion = default);

    /// <summary>
    /// Indica si un operador registró algún pago. Se usa antes de borrar un usuario (#68).
    /// </summary>
    /// <param name="operador">Correo del operador, tal como quedó en los pagos.</param>
    /// <param name="cancelacion">Permite cancelar la operación.</param>
    /// <returns><see langword="true"/> si hay al menos un pago con ese operador.</returns>
    Task<bool> ExistenDelOperadorAsync(string operador, CancellationToken cancelacion = default);
}
