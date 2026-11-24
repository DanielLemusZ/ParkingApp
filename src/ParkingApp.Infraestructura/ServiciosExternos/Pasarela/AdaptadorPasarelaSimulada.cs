using System.Collections.Concurrent;

using ParkingApp.Aplicacion.Pagos;

namespace ParkingApp.Infraestructura.ServiciosExternos.Pasarela;

/// <summary>
/// Pasarela de pagos simulada (patrón Adapter): no usa internet. Hace la misma ida y vuelta que una pasarela real, pero
/// la página de pago es una pantalla de la caja donde el operador elige si el pago se aprueba o se rechaza.
/// Las transacciones viven en memoria, así que se pierden al reiniciar la aplicación.
/// </summary>
public sealed class AdaptadorPasarelaSimulada : IPasarelaDePagos
{
    /// <summary>
    /// Prefijo de las transacciones simuladas, para distinguirlas de las de Wompi.
    /// </summary>
    public const string Prefijo = "SIM-";

    /// <summary>
    /// Pantalla de la caja que hace de página de pago.
    /// </summary>
    public const string RutaDePago = "/caja/pasarela/simulada";

    private readonly ConcurrentDictionary<string, TransaccionSimulada> transacciones =
        new ConcurrentDictionary<string, TransaccionSimulada>(StringComparer.Ordinal);

    /// <inheritdoc/>
    public bool Disponible => true;

    /// <inheritdoc/>
    public Task<InicioDePagoEnPasarela> IniciarAsync(SolicitudDePagoEnPasarela solicitud, CancellationToken cancelacion = default)
    {
        ArgumentNullException.ThrowIfNull(solicitud);

        var id = $"{Prefijo}{Guid.NewGuid():N}";
        transacciones[id] = new TransaccionSimulada(solicitud, EstadoEnPasarela.EnProceso, null);

        var url = new Uri($"{RutaDePago}?transaccion={Uri.EscapeDataString(id)}", UriKind.Relative);
        return Task.FromResult(new InicioDePagoEnPasarela(true, url));
    }

    /// <inheritdoc/>
    public Task<ConsultaDePagoEnPasarela> ConsultarAsync(string transaccionId, CancellationToken cancelacion = default)
    {
        if (!transacciones.TryGetValue(transaccionId, out var transaccion))
        {
            return Task.FromResult(new ConsultaDePagoEnPasarela(
                EstadoEnPasarela.Rechazada,
                transaccionId,
                null,
                0m,
                "La transacción no existe en la pasarela simulada"));
        }

        return Task.FromResult(new ConsultaDePagoEnPasarela(
            transaccion.Estado,
            transaccionId,
            transaccion.Solicitud.Referencia,
            transaccion.Solicitud.Valor,
            transaccion.Motivo));
    }

    /// <summary>
    /// Busca una transacción que espera la decisión del operador, para mostrar su valor y su concepto.
    /// </summary>
    /// <param name="transaccionId">Transacción simulada.</param>
    /// <returns>La solicitud de pago, o <see langword="null"/> si no existe o ya se resolvió.</returns>
    public SolicitudDePagoEnPasarela? BuscarPendiente(string? transaccionId)
    {
        return transaccionId is not null &&
            transacciones.TryGetValue(transaccionId, out var transaccion) &&
            transaccion.Estado == EstadoEnPasarela.EnProceso
            ? transaccion.Solicitud
            : null;
    }

    /// <summary>
    /// Aprueba o rechaza una transacción simulada, como lo haría el banco.
    /// </summary>
    /// <param name="transaccionId">Transacción simulada.</param>
    /// <param name="aprobar">Si el pago se aprueba.</param>
    /// <param name="motivo">Motivo del rechazo.</param>
    /// <returns>La página de retorno de la caja con la transacción, o <see langword="null"/> si no existe.</returns>
    public Uri? Resolver(string transaccionId, bool aprobar, string? motivo = null)
    {
        if (!transacciones.TryGetValue(transaccionId, out var transaccion))
        {
            return null;
        }

        var resuelta = aprobar
            ? transaccion with { Estado = EstadoEnPasarela.Aprobada, Motivo = null }
            : transaccion with { Estado = EstadoEnPasarela.Rechazada, Motivo = string.IsNullOrWhiteSpace(motivo) ? null : motivo.Trim() };
        transacciones[transaccionId] = resuelta;

        var retorno = transaccion.Solicitud.UrlDeRetorno.ToString();
        var separador = retorno.Contains('?', StringComparison.Ordinal) ? '&' : '?';
        return new Uri($"{retorno}{separador}id={Uri.EscapeDataString(transaccionId)}", UriKind.RelativeOrAbsolute);
    }

    private sealed record TransaccionSimulada(SolicitudDePagoEnPasarela Solicitud, EstadoEnPasarela Estado, string? Motivo);
}
