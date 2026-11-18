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
    /// Obtiene la versión que comienza en la fecha indicada, si existe.
    /// </summary>
    /// <param name="tipoDeVehiculo">Tipo de vehículo.</param>
    /// <param name="franja">Franja tarifaria.</param>
    /// <param name="fecha">Fecha de inicio exacta.</param>
    /// <param name="cancelacion">Permite cancelar la operación.</param>
    /// <returns>La versión encontrada o <see langword="null"/>.</returns>
    Task<Tarifa?> ObtenerIniciadaAsync(
        TipoDeVehiculo tipoDeVehiculo,
        FranjaTarifaria franja,
        DateOnly fecha,
        CancellationToken cancelacion = default);

    /// <summary>
    /// Obtiene la siguiente versión futura de una tarifa.
    /// </summary>
    /// <param name="tipoDeVehiculo">Tipo de vehículo.</param>
    /// <param name="franja">Franja tarifaria.</param>
    /// <param name="fecha">Fecha desde la que se busca.</param>
    /// <param name="cancelacion">Permite cancelar la operación.</param>
    /// <returns>La siguiente versión o <see langword="null"/>.</returns>
    Task<Tarifa?> ObtenerSiguienteAsync(
        TipoDeVehiculo tipoDeVehiculo,
        FranjaTarifaria franja,
        DateOnly fecha,
        CancellationToken cancelacion = default);

    /// <summary>
    /// Inserta una nueva versión. Devuelve <see langword="false"/> si ya existe una versión con el mismo tipo, franja y fecha.
    /// </summary>
    /// <param name="tarifa">Nueva versión de la tarifa.</param>
    /// <param name="cancelacion">Permite cancelar la operación.</param>
    /// <returns><see langword="true"/> si se guardó; <see langword="false"/> si ya existía esa fecha de inicio.</returns>
    Task<bool> AgregarAsync(Tarifa tarifa, CancellationToken cancelacion = default);

    /// <summary>
    /// Guarda los cambios de una versión ya consultada y modificada.
    /// </summary>
    /// <param name="tarifa">Versión modificada.</param>
    /// <param name="cancelacion">Permite cancelar la operación.</param>
    /// <returns>Una tarea que termina cuando los cambios quedan guardados.</returns>
    Task GuardarCambiosAsync(Tarifa tarifa, CancellationToken cancelacion = default);
}
