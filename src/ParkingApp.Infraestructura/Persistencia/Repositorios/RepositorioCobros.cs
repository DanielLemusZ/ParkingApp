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

    // PostgreSQL responde con el código 23505 cuando se viola la restricción única del cobro del día.
    private static bool EsCobroDelDiaDuplicado(DbUpdateException error)
    {
        return error.InnerException is PostgresException postgres &&
            postgres.SqlState == PostgresErrorCodes.UniqueViolation &&
            postgres.ConstraintName == CobroConfiguracion.IndiceUnicoDelDia;
    }
}
