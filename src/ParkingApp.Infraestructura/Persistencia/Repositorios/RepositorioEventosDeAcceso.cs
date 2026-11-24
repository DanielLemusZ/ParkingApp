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

    public async Task<IReadOnlyList<VehiculoAdentro>> ListarAdentroAsync(Guid sedeId, CancellationToken cancelacion = default)
    {
        // La misma regla de la ocupación (RepositorioSedes): está adentro si no hay un evento posterior de la misma placa.
        var ultimosIngresos = await contexto.EventosDeAcceso
            .AsNoTracking()
            .Where(ultimo => ultimo.SedeId == sedeId && ultimo.Tipo == TipoDeEvento.Ingreso &&
                !contexto.EventosDeAcceso.Any(posterior =>
                    posterior.Placa == ultimo.Placa && posterior.FechaHora > ultimo.FechaHora))
            .ToListAsync(cancelacion);

        // El record se arma en memoria, porque la placa es un value object con conversión.
        return ultimosIngresos
            .Select(ingreso => new VehiculoAdentro(ingreso.Placa, ingreso.TipoDeVehiculo, ingreso.FechaHora))
            .ToList();
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
