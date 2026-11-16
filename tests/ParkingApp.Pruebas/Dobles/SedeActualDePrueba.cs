using ParkingApp.Aplicacion.Configuracion;

namespace ParkingApp.Pruebas.Dobles;

/// <summary>
/// Sede fija en Bogotá, para las pruebas.
/// </summary>
internal sealed class SedeActualDePrueba : ISedeActual
{
    public Guid Id { get; } = Guid.CreateVersion7();

    public TimeZoneInfo ZonaHoraria { get; } = TimeZoneInfo.FindSystemTimeZoneById("America/Bogota");
}
