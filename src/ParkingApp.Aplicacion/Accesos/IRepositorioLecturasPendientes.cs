using ParkingApp.Dominio.Accesos;

namespace ParkingApp.Aplicacion.Accesos;

/// <summary>
/// Guarda y consulta las lecturas que esperan la validación del operador (F-06).
/// </summary>
public interface IRepositorioLecturasPendientes
{
    /// <summary>
    /// Guarda una lectura pendiente nueva.
    /// </summary>
    /// <param name="lectura">Lectura pendiente.</param>
    /// <param name="cancelacion">Permite cancelar la operación.</param>
    /// <returns>Una tarea que termina cuando la lectura queda guardada.</returns>
    Task AgregarAsync(LecturaPendiente lectura, CancellationToken cancelacion = default);

    /// <summary>
    /// Busca una lectura, pendiente o validada.
    /// </summary>
    /// <param name="id">Identificador de la lectura.</param>
    /// <param name="cancelacion">Permite cancelar la operación.</param>
    /// <returns>La lectura, o nada si no existe.</returns>
    Task<LecturaPendiente?> ObtenerAsync(Guid id, CancellationToken cancelacion = default);

    /// <summary>
    /// Lista las lecturas que siguen pendientes en una sede, de la más antigua a la más reciente.
    /// </summary>
    /// <param name="sedeId">Sede de la caja.</param>
    /// <param name="cancelacion">Permite cancelar la operación.</param>
    /// <returns>Las lecturas pendientes.</returns>
    Task<IReadOnlyList<LecturaPendiente>> ListarPendientesAsync(Guid sedeId, CancellationToken cancelacion = default);

    /// <summary>
    /// Guarda la validación solo si la lectura seguía pendiente: si dos operadores confirman a la vez, gana el primero.
    /// </summary>
    /// <param name="lectura">Lectura que se acaba de validar.</param>
    /// <param name="cancelacion">Permite cancelar la operación.</param>
    /// <returns><see langword="false"/> si otro operador la validó primero.</returns>
    Task<bool> GuardarValidacionAsync(LecturaPendiente lectura, CancellationToken cancelacion = default);

    /// <summary>
    /// Guarda lo que respondió el carril después de validar la lectura.
    /// </summary>
    /// <param name="lectura">Lectura validada, con su resultado.</param>
    /// <param name="cancelacion">Permite cancelar la operación.</param>
    /// <returns>Una tarea que termina cuando el resultado queda guardado.</returns>
    Task GuardarResultadoAsync(LecturaPendiente lectura, CancellationToken cancelacion = default);
}
