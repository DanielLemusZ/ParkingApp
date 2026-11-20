using Microsoft.EntityFrameworkCore;

using ParkingApp.Aplicacion.Reportes;
using ParkingApp.Dominio.Accesos;

namespace ParkingApp.Infraestructura.Persistencia.Consultas;

/// <summary>
/// Implementación del repositorio de consultas en base de datos.
/// </summary>
public class RepositorioConsultas : IRepositorioConsultas
{
    private readonly ParkingAppDbContext _context;

    public RepositorioConsultas(ParkingAppDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc/>
    public async Task<List<EventoDeAcceso>> ConsultarAccesosAsync(DateTimeOffset? fechaInicioUtc, DateTimeOffset? fechaFinUtc, string? placaNormalizada)
    {
        var query = _context.EventosDeAcceso.AsNoTracking().AsQueryable();

        if (fechaInicioUtc.HasValue)
        {
            query = query.Where(a => a.FechaHora >= fechaInicioUtc.Value);
        }

        if (fechaFinUtc.HasValue)
        {
            query = query.Where(a => a.FechaHora < fechaFinUtc.Value);
        }

        if (!string.IsNullOrWhiteSpace(placaNormalizada))
        {
            // El filtro ahora se ejecuta como consulta SQL (LINQ to Entities) y no en la memoria de C#
            query = query.Where(a => a.Placa.Valor.Contains(placaNormalizada));
        }

        return await query
            .OrderByDescending(a => a.FechaHora)
            .ToListAsync();
    }
}
