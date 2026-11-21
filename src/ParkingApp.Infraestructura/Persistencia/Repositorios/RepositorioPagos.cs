using Microsoft.EntityFrameworkCore;

using Npgsql;

using ParkingApp.Aplicacion.Pagos;
using ParkingApp.Dominio.Cobros;
using ParkingApp.Dominio.Pagos;
using ParkingApp.Infraestructura.Persistencia.Configuraciones;

namespace ParkingApp.Infraestructura.Persistencia.Repositorios;

/// <summary>
/// Guarda y consulta los pagos en PostgreSQL con EF Core.
/// </summary>
internal sealed class RepositorioPagos : IRepositorioPagos
{
    private readonly ParkingAppDbContext contexto;

    public RepositorioPagos(ParkingAppDbContext contexto)
    {
        this.contexto = contexto;
    }

    public Task<bool> ExisteReferenciaAsync(Guid referencia, CancellationToken cancelacion = default)
    {
        return contexto.Pagos
            .AsNoTracking()
            .AnyAsync(pago => pago.Referencia == referencia, cancelacion);
    }

    public async Task<bool> AgregarAsync(Pago pago, CancellationToken cancelacion = default)
    {
        contexto.Pagos.Add(pago);

        try
        {
            // El cobro se leyó con seguimiento en este mismo contexto: un solo SaveChanges guarda el pago y el
            // cambio a Pagado en la misma transacción.
            await contexto.SaveChangesAsync(cancelacion);
            return true;
        }
        catch (DbUpdateException error) when (EsPagoDuplicado(error))
        {
            // Otra solicitud ya pagó con esta referencia, o ya pagó este cobro. Se suelta el pago rechazado y el
            // cobro vuelve a como estaba en la base de datos, para que el contexto no los intente guardar otra vez.
            contexto.Entry(pago).State = EntityState.Detached;
            DescartarCambiosDelCobro(pago.CobroId);
            return false;
        }
    }

    // PostgreSQL responde con el código 23505 cuando se viola una restricción única.
    private static bool EsPagoDuplicado(DbUpdateException error)
    {
        return error.InnerException is PostgresException postgres &&
            postgres.SqlState == PostgresErrorCodes.UniqueViolation &&
            (postgres.ConstraintName == PagoConfiguracion.IndiceUnicoDeReferencia ||
                postgres.ConstraintName == PagoConfiguracion.IndiceUnicoDelCobro);
    }

    private void DescartarCambiosDelCobro(Guid cobroId)
    {
        foreach (var entrada in contexto.ChangeTracker.Entries<Cobro>().Where(entrada => entrada.Entity.Id == cobroId))
        {
            entrada.CurrentValues.SetValues(entrada.OriginalValues);
            entrada.State = EntityState.Unchanged;
        }
    }
}
