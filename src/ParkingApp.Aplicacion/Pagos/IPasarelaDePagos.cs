namespace ParkingApp.Aplicacion.Pagos;

/// <summary>
/// Puerto de la pasarela de pagos (tarjeta, Nequi o QR). La caja depende de esta interfaz y no de un proveedor:
/// el adaptador simulado, el sandbox de PayU o una pasarela real se cambian sin tocar los casos de uso.
/// </summary>
public interface IPasarelaDePagos
{
    /// <summary>
    /// Indica si la pasarela se puede usar ahora. Si no, la caja ofrece solo efectivo.
    /// </summary>
    bool Disponible { get; }

    /// <summary>
    /// Prepara el pago y devuelve la página donde el conductor paga. Al terminar, la pasarela vuelve a
    /// <see cref="SolicitudDePagoEnPasarela.UrlDeRetorno"/> con el identificador de la transacción.
    /// </summary>
    /// <param name="solicitud">Referencia, valor y concepto del pago.</param>
    /// <param name="cancelacion">Permite cancelar la operación.</param>
    /// <returns>La página de pago, o que la pasarela no está disponible.</returns>
    Task<InicioDePagoEnPasarela> IniciarAsync(SolicitudDePagoEnPasarela solicitud, CancellationToken cancelacion = default);

    /// <summary>
    /// Consulta en la pasarela cómo quedó una transacción.
    /// </summary>
    /// <param name="transaccionId">Identificador de la transacción que devolvió la pasarela.</param>
    /// <param name="cancelacion">Permite cancelar la operación.</param>
    /// <returns>El estado, la referencia y el valor de la transacción.</returns>
    Task<ConsultaDePagoEnPasarela> ConsultarAsync(string transaccionId, CancellationToken cancelacion = default);
}

/// <summary>
/// Datos que la caja le manda a la pasarela para cobrar.
/// </summary>
/// <param name="Referencia">Referencia del pago, escrita con <see cref="ReferenciaDePasarela.Escribir"/>.</param>
/// <param name="Valor">Valor en pesos colombianos.</param>
/// <param name="Concepto">Descripción del pago, por ejemplo <c>Parqueadero ABC123</c>.</param>
/// <param name="UrlDeRetorno">Página de la caja a la que vuelve la pasarela.</param>
public sealed record SolicitudDePagoEnPasarela(string Referencia, decimal Valor, string Concepto, Uri UrlDeRetorno);

/// <summary>
/// Respuesta de la pasarela al iniciar un pago.
/// </summary>
/// <param name="Disponible">Si la pasarela respondió.</param>
/// <param name="UrlDePago">Página donde el conductor paga, o <see langword="null"/> si la pasarela no está disponible.</param>
public sealed record InicioDePagoEnPasarela(bool Disponible, Uri? UrlDePago);

/// <summary>
/// Cómo quedó una transacción en la pasarela.
/// </summary>
/// <param name="Estado">Estado de la transacción.</param>
/// <param name="TransaccionId">Identificador de la transacción en la pasarela.</param>
/// <param name="Referencia">Referencia que mandó la caja, o <see langword="null"/> si la pasarela no respondió.</param>
/// <param name="Valor">Valor de la transacción en pesos colombianos.</param>
/// <param name="Motivo">Motivo del rechazo que da la pasarela, si lo hay.</param>
public sealed record ConsultaDePagoEnPasarela(
    EstadoEnPasarela Estado,
    string TransaccionId,
    string? Referencia,
    decimal Valor,
    string? Motivo);

/// <summary>
/// Estado de una transacción en la pasarela.
/// </summary>
public enum EstadoEnPasarela
{
    /// <summary>
    /// La pasarela aprobó el pago.
    /// </summary>
    Aprobada,

    /// <summary>
    /// La pasarela rechazó el pago, por ejemplo por una tarjeta sin fondos.
    /// </summary>
    Rechazada,

    /// <summary>
    /// El pago sigue en proceso, por ejemplo mientras el conductor lo aprueba en Nequi.
    /// </summary>
    EnProceso,

    /// <summary>
    /// No hubo respuesta de la pasarela: sin internet, demora o falla del proveedor.
    /// </summary>
    NoDisponible,
}
