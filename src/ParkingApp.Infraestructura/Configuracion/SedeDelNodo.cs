using ParkingApp.Aplicacion.Configuracion;
using ParkingApp.Infraestructura.Persistencia.Configuraciones;

namespace ParkingApp.Infraestructura.Configuracion;

/// <summary>
/// El MVP es un solo nodo con una sede: la sede semilla de la base de datos.
/// </summary>
internal sealed class SedeDelNodo : ISedeActual
{
    public Guid Id => SedeConfiguracion.IdSedeDelMvp;

    public TimeZoneInfo ZonaHoraria { get; } =
        TimeZoneInfo.FindSystemTimeZoneById(SedeConfiguracion.ZonaHorariaDeLaSedeDelMvp);
}
