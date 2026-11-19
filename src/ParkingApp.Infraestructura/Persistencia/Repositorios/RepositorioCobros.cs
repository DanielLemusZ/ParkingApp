using Microsoft.EntityFrameworkCore;

using ParkingApp.Aplicacion.Cobros;
using ParkingApp.Dominio.Accesos;
using ParkingApp.Dominio.Cobros;

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

    public async Task AgregarAsync(Cobro cobro, CancellationToken cancelacion = default)
    {
        contexto.Cobros.Add(cobro);
        await contexto.SaveChangesAsync(cancelacion);
    }
}
