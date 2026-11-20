using Microsoft.EntityFrameworkCore;

using ParkingApp.Aplicacion.Reportes;
using ParkingApp.Dominio.Accesos;
using ParkingApp.Dominio.Cobros;

namespace ParkingApp.Infraestructura.Persistencia.Consultas;

/// <summary>
/// Consultas de solo lectura del administrador, sin seguimiento de cambios.
/// </summary>
public class RepositorioConsultas : IRepositorioConsultas
{
    private readonly ParkingAppDbContext _context;

    /// <summary>
    /// Crea el repositorio con el contexto de la base de datos.
    /// </summary>
    /// <param name="context">Contexto de EF Core.</param>
    public RepositorioConsultas(ParkingAppDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc/>
    public async Task<List<EventoDeAcceso>> ConsultarAccesosAsync(DateTimeOffset? fechaInicioUtc, DateTimeOffset? fechaFinUtc, string? placaNormalizada, CancellationToken cancelacion = default)
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
            // La placa se guarda como texto con una conversión, y EF no sabe traducir a SQL "a.Placa.Valor".
            // El cast a string hace que compare directamente la columna placa: WHERE placa LIKE '%ABC%'.
            query = query.Where(a => ((string)(object)a.Placa).Contains(placaNormalizada));
        }

        return await query
            .OrderByDescending(a => a.FechaHora)
            .ToListAsync(cancelacion);
    }

    /// <inheritdoc/>
    public async Task<List<Cobro>> ConsultarCobrosAsync(DateOnly? diaInicio, DateOnly? diaFin, string? placaNormalizada, EstadoDeCobro? estado, CancellationToken cancelacion = default)
    {
        var query = _context.Cobros.AsNoTracking().AsQueryable();

        if (diaInicio.HasValue)
        {
            query = query.Where(c => c.DiaOperativo >= diaInicio.Value);
        }

        if (diaFin.HasValue)
        {
            query = query.Where(c => c.DiaOperativo <= diaFin.Value);
        }

        if (!string.IsNullOrWhiteSpace(placaNormalizada))
        {
            // Mismo cast que en los accesos, por la misma razón.
            query = query.Where(c => ((string)(object)c.Placa).Contains(placaNormalizada));
        }

        if (estado.HasValue)
        {
            query = query.Where(c => c.Estado == estado.Value);
        }

        return await query
            .OrderByDescending(c => c.DiaOperativo)
            .ThenByDescending(c => c.FechaHora)
            .ToListAsync(cancelacion);
    }
}
