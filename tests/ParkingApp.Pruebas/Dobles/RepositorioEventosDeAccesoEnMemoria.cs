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

    public Task<bool> FiguraAdentroAsync(Placa placa, Guid sedeId, CancellationToken cancelacion = default)
    {
        EventoDeAcceso? ultimo = null;

        foreach (var evento in eventos)
        {
            if (evento.Placa == placa && evento.SedeId == sedeId && (ultimo is null || evento.FechaHora >= ultimo.FechaHora))
            {
                ultimo = evento;
            }
        }

        return Task.FromResult(ultimo is not null && ultimo.Tipo == TipoDeEvento.Ingreso);
    }
}
