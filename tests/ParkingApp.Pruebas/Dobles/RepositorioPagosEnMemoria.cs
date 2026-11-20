using ParkingApp.Aplicacion.Pagos;
using ParkingApp.Dominio.Pagos;

namespace ParkingApp.Pruebas.Dobles;

/// <summary>
/// Repositorio de pagos en memoria, para probar los casos de uso sin base de datos.
/// Imita la restricción única de la referencia del pago (idempotencia).
/// </summary>
internal sealed class RepositorioPagosEnMemoria : IRepositorioPagos
{
    private readonly List<Pago> pagos = new List<Pago>();

    public IReadOnlyList<Pago> Pagos => pagos;

    /// <summary>
    /// Simula un doble clic: la consulta no ve el pago que la otra solicitud acaba de guardar,
    /// pero al guardar sí se aplica la restricción única, como en PostgreSQL.
    /// </summary>
    public bool OtraSolicitudGuardoElPagoDespuesDeLaConsulta { get; set; }

    public Task<bool> ExisteReferenciaAsync(Guid referencia, CancellationToken cancelacion = default)
    {
        var existe = !OtraSolicitudGuardoElPagoDespuesDeLaConsulta && pagos.Any(pago => pago.Referencia == referencia);
        return Task.FromResult(existe);
    }

    public Task<bool> AgregarAsync(Pago pago, CancellationToken cancelacion = default)
    {
        if (pagos.Any(guardado => guardado.Referencia == pago.Referencia))
        {
            return Task.FromResult(false);
        }

        pagos.Add(pago);
        return Task.FromResult(true);
    }

    /// <summary>
    /// Guarda un pago como si lo hubiera registrado otra solicitud, para simular un doble clic.
    /// </summary>
    public void AgregarDeOtraSolicitud(Pago pago)
    {
        pagos.Add(pago);
    }
}
