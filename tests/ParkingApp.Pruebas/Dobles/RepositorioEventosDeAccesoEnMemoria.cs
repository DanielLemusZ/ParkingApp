using ParkingApp.Aplicacion.Accesos;
using ParkingApp.Dominio.Accesos;

namespace ParkingApp.Pruebas.Dobles;

/// <summary>
/// Repositorio de eventos en memoria, para probar los casos de uso sin base de datos.
/// </summary>
internal sealed class RepositorioEventosDeAccesoEnMemoria : IRepositorioEventosDeAcceso
{
    private readonly List<EventoDeAcceso> eventos = new List<EventoDeAcceso>();

    public IReadOnlyList<EventoDeAcceso> Eventos => eventos;

    public Task AgregarAsync(EventoDeAcceso evento, CancellationToken cancelacion = default)
    {
        eventos.Add(evento);
        return Task.CompletedTask;
    }
}
