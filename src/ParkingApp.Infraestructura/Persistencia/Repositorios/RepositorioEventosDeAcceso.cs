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
}
