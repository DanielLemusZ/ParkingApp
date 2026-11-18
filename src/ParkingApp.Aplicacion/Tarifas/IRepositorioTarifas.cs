using ParkingApp.Dominio.Accesos;
using ParkingApp.Dominio.Configuracion;

namespace ParkingApp.Aplicacion.Tarifas;

/// <summary>
/// Consulta y registra versiones de las tarifas.
/// </summary>
public interface IRepositorioTarifas
{
    /// <summary>
    /// Obtiene todas las tarifas vigentes en una fecha.
    /// </summary>
    /// <param name="fecha">Fecha que se consulta.</param>
    /// <param name="cancelacion">Permite cancelar la operación.</param>
    /// <returns>Tarifas vigentes para cada tipo y franja.</returns>
    Task<IReadOnlyList<Tarifa>> ObtenerVigentesAsync(DateOnly fecha, CancellationToken cancelacion = default);

    /// <summary>
    /// Obtiene una tarifa vigente para el tipo, franja y fecha indicados.
    /// </summary>
    /// <param name="tipoDeVehiculo">Tipo de vehículo.</param>
    /// <param name="franja">Franja tarifaria.</param>
    /// <param name="fecha">Fecha que se consulta.</param>
    /// <param name="cancelacion">Permite cancelar la operación.</param>
    /// <returns>La tarifa vigente o <see langword="null"/> si no existe.</returns>
    Task<Tarifa?> ObtenerVigenteAsync(
        TipoDeVehiculo tipoDeVehiculo,
        FranjaTarifaria franja,
        DateOnly fecha,
        CancellationToken cancelacion = default);

    /// <summary>
    /// Guarda una nueva versión y conserva las versiones anteriores.
    /// </summary>
    /// <param name="tarifa">Nueva versión de la tarifa.</param>
    /// <param name="cancelacion">Permite cancelar la operación.</param>
    /// <returns>Una tarea que termina cuando la versión queda guardada.</returns>
    Task AgregarVersionAsync(Tarifa tarifa, CancellationToken cancelacion = default);
}
