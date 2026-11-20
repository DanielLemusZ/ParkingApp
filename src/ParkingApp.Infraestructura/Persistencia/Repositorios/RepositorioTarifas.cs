using Microsoft.EntityFrameworkCore;

using Npgsql;

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
            .SingleOrDefaultAsync(
                tarifa => tarifa.TipoDeVehiculo == tipoDeVehiculo &&
                    tarifa.Franja == franja &&
                    tarifa.VigenteDesde <= fecha &&
                    (tarifa.VigenteHasta == null || tarifa.VigenteHasta >= fecha),
                cancelacion);
    }

    public Task<Tarifa?> ObtenerIniciadaAsync(
        TipoDeVehiculo tipoDeVehiculo,
        FranjaTarifaria franja,
        DateOnly fecha,
        CancellationToken cancelacion = default)
    {
        return contexto.Tarifas.SingleOrDefaultAsync(
            tarifa => tarifa.TipoDeVehiculo == tipoDeVehiculo &&
                tarifa.Franja == franja &&
                tarifa.VigenteDesde == fecha,
            cancelacion);
    }

    public Task<Tarifa?> ObtenerSiguienteAsync(
        TipoDeVehiculo tipoDeVehiculo,
        FranjaTarifaria franja,
        DateOnly fecha,
        CancellationToken cancelacion = default)
    {
        return contexto.Tarifas
            .Where(tarifa => tarifa.TipoDeVehiculo == tipoDeVehiculo &&
                tarifa.Franja == franja &&
                tarifa.VigenteDesde > fecha)
            .OrderBy(tarifa => tarifa.VigenteDesde)
            .FirstOrDefaultAsync(cancelacion);
    }

    public async Task<bool> AgregarAsync(Tarifa tarifa, CancellationToken cancelacion = default)
    {
        await using var transaccion = await contexto.Database.BeginTransactionAsync(cancelacion);
        contexto.Tarifas.Add(tarifa);

        try
        {
            await contexto.SaveChangesAsync(cancelacion);
            await transaccion.CommitAsync(cancelacion);
            return true;
        }
        catch (DbUpdateException error) when (EsFechaDuplicada(error))
        {
            await transaccion.RollbackAsync(cancelacion);
            contexto.Entry(tarifa).State = EntityState.Detached;
            return false;
        }
    }

    public async Task GuardarCambiosAsync(Tarifa tarifa, CancellationToken cancelacion = default)
    {
        await contexto.SaveChangesAsync(cancelacion);
    }

    private static bool EsFechaDuplicada(DbUpdateException error)
    {
        return error.InnerException is PostgresException postgres &&
            postgres.SqlState == PostgresErrorCodes.UniqueViolation &&
            postgres.ConstraintName == "ix_tarifas_tipo_de_vehiculo_franja_vigente_desde";
    }
}
