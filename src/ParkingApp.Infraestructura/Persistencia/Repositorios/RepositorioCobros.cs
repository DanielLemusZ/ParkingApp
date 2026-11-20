using Microsoft.EntityFrameworkCore;

using Npgsql;

using ParkingApp.Aplicacion.Cobros;
using ParkingApp.Dominio.Accesos;
using ParkingApp.Dominio.Cobros;
using ParkingApp.Infraestructura.Persistencia.Configuraciones;

namespace ParkingApp.Infraestructura.Persistencia.Repositorios;

/// <summary>
/// Guarda y consulta los cobros en PostgreSQL con EF Core.
/// </summary>
internal sealed class RepositorioCobros : IRepositorioCobros
{
    private readonly ParkingAppDbContext contexto;

    public RepositorioCobros(ParkingAppDbContext contexto)
    {
        this.contexto = contexto;
    }

    public Task<bool> ExisteDelDiaAsync(Placa placa, Guid sedeId, DateOnly diaOperativo, CancellationToken cancelacion = default)
    {
        return contexto.Cobros
            .AsNoTracking()
            .AnyAsync(
                cobro => cobro.Placa == placa && cobro.SedeId == sedeId && cobro.DiaOperativo == diaOperativo,
                cancelacion);
    }

    public async Task<bool> AgregarAsync(Cobro cobro, CancellationToken cancelacion = default)
    {
        contexto.Cobros.Add(cobro);

        try
        {
            await contexto.SaveChangesAsync(cancelacion);
            return true;
        }
        catch (DbUpdateException error) when (EsCobroDelDiaDuplicado(error))
        {
            // Otro ingreso de la misma placa guardó primero el cobro del día (RN-04). Se suelta el cobro
            // rechazado para que el contexto no lo vuelva a intentar guardar.
            contexto.Entry(cobro).State = EntityState.Detached;
            return false;
        }
    }

    public Task<decimal> SumarPendientesAsync(Placa placa, Guid sedeId, CancellationToken cancelacion = default)
    {
        return contexto.Cobros
            .AsNoTracking()
            .Where(cobro => cobro.Placa == placa && cobro.SedeId == sedeId && cobro.Estado == EstadoDeCobro.Pendiente)
            .SumAsync(cobro => cobro.Valor, cancelacion);
    }

    public Task<bool> ExistePagadoDelDiaAsync(Placa placa, Guid sedeId, DateOnly diaOperativo, CancellationToken cancelacion = default)
    {
        return contexto.Cobros
            .AsNoTracking()
            .AnyAsync(
                cobro => cobro.Placa == placa && cobro.SedeId == sedeId && cobro.DiaOperativo == diaOperativo &&
                    cobro.Estado == EstadoDeCobro.Pagado,
                cancelacion);
    }

    public Task<Cobro?> ObtenerAsync(Guid id, CancellationToken cancelacion = default)
    {
        // Con seguimiento: el cambio a Pagado se guarda junto con el pago, en el mismo SaveChanges.
        return contexto.Cobros.FirstOrDefaultAsync(cobro => cobro.Id == id, cancelacion);
    }

    public async Task<IReadOnlyList<Cobro>> ListarPendientesAsync(Guid sedeId, string placaContiene, CancellationToken cancelacion = default)
    {
        var pendientes = await contexto.Cobros
            .AsNoTracking()
            .Where(cobro => cobro.SedeId == sedeId && cobro.Estado == EstadoDeCobro.Pendiente)
            .OrderBy(cobro => cobro.FechaHora)
            .ToListAsync(cancelacion);

        // La placa es un value object con conversión, y EF no traduce Placa.Valor.Contains a SQL. Los pendientes
        // de una sede son pocos (los vehículos que deben), así que el filtro por placa se hace en memoria.
        return pendientes
            .Where(cobro => cobro.Placa.Valor.Contains(placaContiene, StringComparison.Ordinal))
            .ToList();
    }

    // PostgreSQL responde con el código 23505 cuando se viola la restricción única del cobro del día.
    private static bool EsCobroDelDiaDuplicado(DbUpdateException error)
    {
        return error.InnerException is PostgresException postgres &&
            postgres.SqlState == PostgresErrorCodes.UniqueViolation &&
            postgres.ConstraintName == CobroConfiguracion.IndiceUnicoDelDia;
    }
}
