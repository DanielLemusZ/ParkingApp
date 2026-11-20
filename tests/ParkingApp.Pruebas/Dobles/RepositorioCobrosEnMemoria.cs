using ParkingApp.Aplicacion.Cobros;
using ParkingApp.Dominio.Accesos;
using ParkingApp.Dominio.Cobros;

namespace ParkingApp.Pruebas.Dobles;

/// <summary>
/// Repositorio de cobros en memoria, para probar los casos de uso sin base de datos.
/// </summary>
internal sealed class RepositorioCobrosEnMemoria : IRepositorioCobros
{
    private readonly List<Cobro> cobros = new List<Cobro>();

    public IReadOnlyList<Cobro> Cobros => cobros;

    public Task<bool> ExisteDelDiaAsync(Placa placa, Guid sedeId, DateOnly diaOperativo, CancellationToken cancelacion = default)
    {
        var existe = cobros.Any(cobro => cobro.Placa == placa && cobro.SedeId == sedeId && cobro.DiaOperativo == diaOperativo);
        return Task.FromResult(existe);
    }

    public Task AgregarAsync(Cobro cobro, CancellationToken cancelacion = default)
    {
        cobros.Add(cobro);
        return Task.CompletedTask;
    }
}
