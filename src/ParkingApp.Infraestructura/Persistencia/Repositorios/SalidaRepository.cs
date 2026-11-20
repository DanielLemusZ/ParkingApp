using Microsoft.EntityFrameworkCore;

using ParkingApp.Aplicacion.Accesos;
using ParkingApp.Dominio.Accesos;
using ParkingApp.Dominio.Cobros;

namespace ParkingApp.Infraestructura.Persistencia.Repositorios;

/// <summary>
/// Implementación de persistencia para las operaciones de salida y consulta de cobros.
/// </summary>
public sealed class SalidaRepository : ISalidaRepository
{
    private const int UmbralConfianzaPorDefecto = 80;
    private readonly ParkingAppDbContext _contexto;

    /// <summary>
    /// Inicializa una nueva instancia del repositorio de salidas.
    /// </summary>
    /// <param name="contexto">Contexto de base de datos de la aplicación.</param>
    public SalidaRepository(ParkingAppDbContext contexto)
    {
        _contexto = contexto;
    }

    /// <inheritdoc />
    public async Task<decimal?> ObtenerValorCobroPendienteAsync(Placa placa, CancellationToken cancelacion = default)
    {
        var cobro = await _contexto.Cobros
            .AsNoTracking()
            .Where(c => c.Placa == placa && c.Estado == EstadoDeCobro.Pendiente)
            .OrderBy(c => c.FechaHora)
            .FirstOrDefaultAsync(cancelacion);

        return cobro?.Valor;
    }

    /// <inheritdoc />
    public Task<bool> TieneCobroPagadoEnDiaOperativoAsync(Placa placa, DateOnly diaOperativo, CancellationToken cancelacion = default)
    {
        return _contexto.Cobros
            .AsNoTracking()
            .AnyAsync(
                c => c.Placa == placa &&
                     c.Estado == EstadoDeCobro.Pagado &&
                     c.DiaOperativo == diaOperativo,
                cancelacion);
    }

    /// <inheritdoc />
    public async Task<int> ObtenerUmbralConfianzaAsync(Guid sedeId, CancellationToken cancelacion = default)
    {
        var umbral = await _contexto.Sedes
            .AsNoTracking()
            .Where(s => s.Id == sedeId)
            .Select(s => (int?)s.UmbralConfianza)
            .FirstOrDefaultAsync(cancelacion);

        return umbral ?? UmbralConfianzaPorDefecto;
    }

    /// <inheritdoc />
    public async Task RegistrarSalidaAsync(
        Placa placa,
        TipoDeVehiculo tipoDeVehiculo,
        DateTimeOffset fechaHoraUtc,
        Guid sedeId,
        int carril,
        int confianza,
        CancellationToken cancelacion = default)
    {
        var eventoSalida = EventoDeAcceso.RegistrarSalida(
            placa,
            tipoDeVehiculo,
            sedeId,
            carril,
            confianza,
            fechaHoraUtc);

        _contexto.EventosDeAcceso.Add(eventoSalida);
        await _contexto.SaveChangesAsync(cancelacion);
    }
}
