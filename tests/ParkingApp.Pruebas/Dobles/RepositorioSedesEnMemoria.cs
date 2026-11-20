using ParkingApp.Aplicacion.Configuracion;
using ParkingApp.Dominio.Configuracion;

namespace ParkingApp.Pruebas.Dobles;

/// <summary>Repositorio en memoria para probar el caso de uso de configuración de la sede.</summary>
internal sealed class RepositorioSedesEnMemoria : IRepositorioSedes
{
    private readonly Sede sede;

    public RepositorioSedesEnMemoria(Sede sede, OcupacionActual? ocupacion = null)
    {
        this.sede = sede;
        Ocupacion = ocupacion ?? new OcupacionActual(0, 0);
    }

    public OcupacionActual Ocupacion { get; set; }

    public int VecesGuardado { get; private set; }

    public Task<Sede> ObtenerAsync(CancellationToken cancelacion = default)
    {
        return Task.FromResult(sede);
    }

    public Task GuardarCambiosAsync(Sede sedeActualizada, CancellationToken cancelacion = default)
    {
        Assert.Same(sede, sedeActualizada);
        VecesGuardado++;
        return Task.CompletedTask;
    }

    public Task<OcupacionActual> ConsultarOcupacionAsync(CancellationToken cancelacion = default)
    {
        return Task.FromResult(Ocupacion);
    }
}
