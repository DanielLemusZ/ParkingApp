using ParkingApp.Dominio.Configuracion;

namespace ParkingApp.Aplicacion.Configuracion;

/// <summary>
/// Horario de operación vigente en la sede: el inicio de la franja diurna y el de la nocturna, que configura el administrador.
/// </summary>
public interface IHorarioDeOperacionActual
{
    /// <summary>
    /// Obtiene el horario que se usa para calcular el día operativo y la franja de cada ingreso.
    /// </summary>
    /// <param name="cancelacion">Permite cancelar la operación.</param>
    /// <returns>El horario vigente de la sede.</returns>
    Task<HorarioDeOperacion> ObtenerAsync(CancellationToken cancelacion = default);
}
