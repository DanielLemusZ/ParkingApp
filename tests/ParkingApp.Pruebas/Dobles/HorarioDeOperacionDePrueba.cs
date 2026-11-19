using ParkingApp.Aplicacion.Configuracion;
using ParkingApp.Dominio.Configuracion;

namespace ParkingApp.Pruebas.Dobles;

/// <summary>
/// Horario de la sede para las pruebas. Empieza con el inicial del MVP (06:00 y 18:00), y cada prueba lo puede cambiar
/// como lo haría el administrador.
/// </summary>
internal sealed class HorarioDeOperacionDePrueba : IHorarioDeOperacionActual
{
    public HorarioDeOperacion Horario { get; set; } = HorarioDeOperacion.Inicial;

    public Task<HorarioDeOperacion> ObtenerAsync(CancellationToken cancelacion = default)
    {
        return Task.FromResult(Horario);
    }
}
