using ParkingApp.Aplicacion.Configuracion;
using ParkingApp.Dominio.Configuracion;

namespace ParkingApp.Pruebas.Dobles;

/// <summary>
/// Horario inicial del MVP (06:00 y 18:00), para las pruebas.
/// </summary>
internal sealed class HorarioDeOperacionDePrueba : IHorarioDeOperacionActual
{
    public HorarioDeOperacion Horario => HorarioDeOperacion.Inicial;
}
