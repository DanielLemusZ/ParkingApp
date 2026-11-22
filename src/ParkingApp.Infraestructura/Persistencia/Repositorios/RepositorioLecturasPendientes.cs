using Microsoft.EntityFrameworkCore;

using ParkingApp.Aplicacion.Accesos;
using ParkingApp.Dominio.Accesos;

namespace ParkingApp.Infraestructura.Persistencia.Repositorios;

/// <summary>
/// Guarda y consulta en PostgreSQL las lecturas que valida el operador, con EF Core.
/// </summary>
internal sealed class RepositorioLecturasPendientes : IRepositorioLecturasPendientes
{
    private readonly ParkingAppDbContext contexto;

    public RepositorioLecturasPendientes(ParkingAppDbContext contexto)
    {
        this.contexto = contexto;
    }

    public async Task AgregarAsync(LecturaPendiente lectura, CancellationToken cancelacion = default)
    {
        contexto.LecturasPendientes.Add(lectura);
        await contexto.SaveChangesAsync(cancelacion);
    }

    public Task<LecturaPendiente?> ObtenerAsync(Guid id, CancellationToken cancelacion = default)
    {
        // Con seguimiento: EF recuerda el estado que leyó y lo compara al guardar la validación.
        return contexto.LecturasPendientes.FirstOrDefaultAsync(lectura => lectura.Id == id, cancelacion);
    }

    public async Task<IReadOnlyList<LecturaPendiente>> ListarPendientesAsync(Guid sedeId, CancellationToken cancelacion = default)
    {
        return await contexto.LecturasPendientes
            .AsNoTracking()
            .Where(lectura => lectura.SedeId == sedeId && lectura.Estado == EstadoDeLectura.Pendiente)
            .OrderBy(lectura => lectura.FechaHora)
            .ToListAsync(cancelacion);
    }

    public async Task<bool> GuardarValidacionAsync(LecturaPendiente lectura, CancellationToken cancelacion = default)
    {
        try
        {
            await contexto.SaveChangesAsync(cancelacion);
            return true;
        }
        catch (DbUpdateConcurrencyException)
        {
            // Otro operador la validó primero. Se suelta para que el contexto no la intente guardar otra vez.
            contexto.Entry(lectura).State = EntityState.Detached;
            return false;
        }
    }

    public Task GuardarResultadoAsync(LecturaPendiente lectura, CancellationToken cancelacion = default)
    {
        return contexto.SaveChangesAsync(cancelacion);
    }
}
