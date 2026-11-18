using Microsoft.EntityFrameworkCore;

using ParkingApp.Aplicacion.Tarifas;
using ParkingApp.Dominio.Accesos;
using ParkingApp.Dominio.Configuracion;

namespace ParkingApp.Infraestructura.Persistencia.Repositorios;

/// <summary>
/// Consulta y guarda versiones de tarifas en PostgreSQL.
/// </summary>
internal sealed class RepositorioTarifas : IRepositorioTarifas
{
    private readonly ParkingAppDbContext contexto;

    public RepositorioTarifas(ParkingAppDbContext contexto)
    {
        this.contexto = contexto;
    }

    public async Task<IReadOnlyList<Tarifa>> ObtenerVigentesAsync(DateOnly fecha, CancellationToken cancelacion = default)
    {
        return await contexto.Tarifas
            .AsNoTracking()
            .Where(tarifa => tarifa.VigenteDesde <= fecha && (tarifa.VigenteHasta == null || tarifa.VigenteHasta >= fecha))
            .OrderBy(tarifa => tarifa.TipoDeVehiculo)
            .ThenBy(tarifa => tarifa.Franja)
            .ToListAsync(cancelacion);
    }

    public Task<Tarifa?> ObtenerVigenteAsync(
        TipoDeVehiculo tipoDeVehiculo,
        FranjaTarifaria franja,
        DateOnly fecha,
        CancellationToken cancelacion = default)
    {
        return contexto.Tarifas
            .AsNoTracking()
            .SingleOrDefaultAsync(
                tarifa => tarifa.TipoDeVehiculo == tipoDeVehiculo &&
                    tarifa.Franja == franja &&
                    tarifa.VigenteDesde <= fecha &&
                    (tarifa.VigenteHasta == null || tarifa.VigenteHasta >= fecha),
                cancelacion);
    }

    public async Task AgregarVersionAsync(Tarifa tarifa, CancellationToken cancelacion = default)
    {
        await using var transaccion = await contexto.Database.BeginTransactionAsync(cancelacion);
        var anteriores = await contexto.Tarifas
            .Where(actual => actual.TipoDeVehiculo == tarifa.TipoDeVehiculo &&
                actual.Franja == tarifa.Franja &&
                actual.VigenteDesde < tarifa.VigenteDesde &&
                (actual.VigenteHasta == null || actual.VigenteHasta >= tarifa.VigenteDesde))
            .OrderByDescending(actual => actual.VigenteDesde)
            .ToListAsync(cancelacion);

        var siguiente = await contexto.Tarifas
            .Where(actual => actual.TipoDeVehiculo == tarifa.TipoDeVehiculo &&
                actual.Franja == tarifa.Franja &&
                actual.VigenteDesde > tarifa.VigenteDesde)
            .OrderBy(actual => actual.VigenteDesde)
            .FirstOrDefaultAsync(cancelacion);

        if (anteriores.Count > 0)
        {
            anteriores[0].FinalizarVigencia(tarifa.VigenteDesde.AddDays(-1));
        }

        if (siguiente is not null)
        {
            tarifa.FinalizarVigencia(siguiente.VigenteDesde.AddDays(-1));
        }

        contexto.Tarifas.Add(tarifa);
        await contexto.SaveChangesAsync(cancelacion);
        await transaccion.CommitAsync(cancelacion);
    }
}
