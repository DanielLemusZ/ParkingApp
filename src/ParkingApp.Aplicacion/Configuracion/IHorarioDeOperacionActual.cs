using ParkingApp.Dominio.Configuracion;

namespace ParkingApp.Aplicacion.Configuracion;

/// <summary>
/// Horario de operación vigente en la sede: inicio del día operativo y de la franja nocturna.
/// </summary>
public interface IHorarioDeOperacionActual
{
    /// <summary>
    /// Horario que se usa para calcular el día operativo y la franja de cada ingreso.
    /// </summary>
    HorarioDeOperacion Horario { get; }
}
