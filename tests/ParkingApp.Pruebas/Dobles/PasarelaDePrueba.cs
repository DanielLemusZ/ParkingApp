using ParkingApp.Aplicacion.Pagos;

namespace ParkingApp.Pruebas.Dobles;

/// <summary>
/// Pasarela de pagos de prueba: guarda la última solicitud y responde la consulta que se le indique.
/// </summary>
internal sealed class PasarelaDePrueba : IPasarelaDePagos
{
    public bool Disponible { get; set; } = true;

    /// <summary>
    /// Simula que la pasarela no responde al iniciar el pago.
    /// </summary>
    public bool FallaAlIniciar { get; set; }

    public SolicitudDePagoEnPasarela? UltimaSolicitud { get; private set; }

    /// <summary>
    /// Lo que responde la consulta. Sin respuesta, la pasarela no está disponible.
    /// </summary>
    public ConsultaDePagoEnPasarela? Respuesta { get; set; }

    public Task<InicioDePagoEnPasarela> IniciarAsync(SolicitudDePagoEnPasarela solicitud, CancellationToken cancelacion = default)
    {
        UltimaSolicitud = solicitud;

        return Task.FromResult(FallaAlIniciar
            ? new InicioDePagoEnPasarela(false, null)
            : new InicioDePagoEnPasarela(true, new Uri("https://pasarela.test/pagar")));
    }

    public Task<ConsultaDePagoEnPasarela> ConsultarAsync(string transaccionId, CancellationToken cancelacion = default)
    {
        return Task.FromResult(Respuesta ?? new ConsultaDePagoEnPasarela(EstadoEnPasarela.NoDisponible, transaccionId, null, 0m, null));
    }

    public void Responder(EstadoEnPasarela estado, string? referencia, decimal valor, string transaccion = "TX-1", string? motivo = null)
    {
        Respuesta = new ConsultaDePagoEnPasarela(estado, transaccion, referencia, valor, motivo);
    }
}
