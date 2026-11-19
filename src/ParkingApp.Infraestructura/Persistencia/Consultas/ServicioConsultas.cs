using System.Globalization;

using Microsoft.EntityFrameworkCore;

using ParkingApp.Aplicacion.Reportes;
using ParkingApp.Dominio.Accesos;
using ParkingApp.Infraestructura.Persistencia;

namespace ParkingApp.Infraestructura.Persistencia.Consultas;

/// <summary>
/// Implementación del servicio de consultas de reportes.
/// </summary>
public class ServicioConsultas : IServicioConsultas
{
    private readonly ParkingAppDbContext _context;

    /// <summary>
    /// Crea una nueva instancia de <see cref="ServicioConsultas"/>.
    /// </summary>
    /// <param name="context">El contexto de la base de datos.</param>
    public ServicioConsultas(ParkingAppDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc/>
    public async Task<List<EventoAccesoDto>> ConsultarAccesosAsync(FiltrosConsultaDto filtros)
    {
        var query = _context.EventosDeAcceso.AsNoTracking().AsQueryable();

        if (filtros.FechaInicio.HasValue)
        {
            var fechaInicioUtc = new DateTimeOffset(filtros.FechaInicio.Value.Date, TimeSpan.FromHours(-5)).ToUniversalTime();
            query = query.Where(a => a.FechaHora >= fechaInicioUtc);
        }

        if (filtros.FechaFin.HasValue)
        {
            var finDelDiaUtc = new DateTimeOffset(filtros.FechaFin.Value.Date.AddDays(1), TimeSpan.FromHours(-5)).ToUniversalTime();
            query = query.Where(a => a.FechaHora < finDelDiaUtc);
        }

        var resultadosDb = await query
            .OrderByDescending(a => a.FechaHora)
            .ToListAsync();

        IEnumerable<EventoDeAcceso> eventosFiltrados = resultadosDb;

        if (!string.IsNullOrWhiteSpace(filtros.Placa))
        {
            var placaNormalizada = Placa.Normalizar(filtros.Placa);
            eventosFiltrados = eventosFiltrados.Where(a => a.Placa.Valor.Contains(placaNormalizada));
        }

        return eventosFiltrados.Select(a => new EventoAccesoDto
        {
            Placa = a.Placa.Valor,
            TipoVehiculo = a.TipoDeVehiculo.ToString(),
            TipoEvento = a.Tipo.ToString(),
            FechaHora = a.FechaHora.ToOffset(TimeSpan.FromHours(-5)).DateTime,
            Carril = a.Carril.ToString(CultureInfo.InvariantCulture),
            EsLecturaManual = false
        }).ToList();
    }

    /// <inheritdoc/>
    public Task<List<CobroConsultaDto>> ConsultarCobrosAsync(FiltrosConsultaDto filtros)
    {
        return Task.FromResult(new List<CobroConsultaDto>());
    }
}
