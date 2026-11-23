using Microsoft.EntityFrameworkCore;

using ParkingApp.Aplicacion.Accesos;
using ParkingApp.Dominio.Accesos;

namespace ParkingApp.Infraestructura.Persistencia.Repositorios;

/// <summary>
/// Guarda los ingresos y las salidas en PostgreSQL con EF Core.
/// </summary>
internal sealed class RepositorioEventosDeAcceso : IRepositorioEventosDeAcceso
{
    private readonly ParkingAppDbContext contexto;

    public RepositorioEventosDeAcceso(ParkingAppDbContext contexto)
    {
        this.contexto = contexto;
    }

    public async Task AgregarAsync(EventoDeAcceso evento, CancellationToken cancelacion = default)
    {
        contexto.EventosDeAcceso.Add(evento);
        await contexto.SaveChangesAsync(cancelacion);
    }

    public async Task<bool> FiguraAdentroAsync(Placa placa, Guid sedeId, CancellationToken cancelacion = default)
    {
        // Se pide el tipo como opcional: sin eventos llega null, y no el primer valor del enum (Ingreso).
        var ultimo = await contexto.EventosDeAcceso
            .AsNoTracking()
            .Where(evento => evento.Placa == placa && evento.SedeId == sedeId)
            .OrderByDescending(evento => evento.FechaHora)
            .Select(evento => (TipoDeEvento?)evento.Tipo)
            .FirstOrDefaultAsync(cancelacion);

        return ultimo == TipoDeEvento.Ingreso;
    }
}
