using ParkingApp.Aplicacion.Accesos;

namespace ParkingApp.Aplicacion.Configuracion;

/// <summary>
/// Caso de uso de la página <em>Cupos</em>: junta los cupos de la sede, la ocupación por conteo (la misma de
/// "LLENO") y los vehículos que siguen adentro. Solo consulta; no cambia nada.
/// </summary>
public sealed class ConsultarCupos
{
    private readonly IRepositorioSedes sedes;
    private readonly IRepositorioEventosDeAcceso eventos;
    private readonly ISedeActual sede;

    /// <summary>
    /// Crea el caso de uso con sus dependencias.
    /// </summary>
    /// <param name="sedes">Cupos configurados y ocupación actual.</param>
    /// <param name="eventos">Donde se consultan los vehículos que siguen adentro.</param>
    /// <param name="sede">Sede donde corre el nodo.</param>
    public ConsultarCupos(IRepositorioSedes sedes, IRepositorioEventosDeAcceso eventos, ISedeActual sede)
    {
        this.sedes = sedes;
        this.eventos = eventos;
        this.sede = sede;
    }

    /// <summary>
    /// Consulta la ocupación de cada tipo y la lista de vehículos adentro.
    /// </summary>
    /// <param name="cancelacion">Permite cancelar la operación.</param>
    /// <returns>El panorama de cupos, con los vehículos ordenados por hora de ingreso.</returns>
    public async Task<PanoramaDeCupos> ConsultarAsync(CancellationToken cancelacion = default)
    {
        var configuracion = await sedes.ObtenerAsync(cancelacion);
        var ocupacion = await sedes.ConsultarOcupacionAsync(cancelacion);
        var adentro = await eventos.ListarAdentroAsync(sede.Id, cancelacion);

        return new PanoramaDeCupos(
            new EstadoDeCupo(configuracion.CupoCarros, ocupacion.Carros),
            new EstadoDeCupo(configuracion.CupoMotos, ocupacion.Motos),
            adentro.OrderBy(vehiculo => vehiculo.FechaHoraDeIngreso).ToList());
    }
}
