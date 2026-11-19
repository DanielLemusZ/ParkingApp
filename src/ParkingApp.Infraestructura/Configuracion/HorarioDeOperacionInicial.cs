using ParkingApp.Aplicacion.Configuracion;
using ParkingApp.Dominio.Configuracion;

namespace ParkingApp.Infraestructura.Configuracion;

/// <summary>
/// Horario inicial del MVP (06:00 y 18:00). Cuando el administrador pueda cambiar las franjas,
/// esta clase se reemplaza por una que lea la configuración guardada, sin tocar los casos de uso.
/// </summary>
internal sealed class HorarioDeOperacionInicial : IHorarioDeOperacionActual
{
    public HorarioDeOperacion Horario => HorarioDeOperacion.Inicial;
}
