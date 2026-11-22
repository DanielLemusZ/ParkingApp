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

    public Task<IReadOnlyList<VehiculoAdentro>> ListarAdentroAsync(Guid sedeId, CancellationToken cancelacion = default)
    {
        IReadOnlyList<VehiculoAdentro> adentro = eventos
            .Where(evento => evento.SedeId == sedeId)
            .GroupBy(evento => evento.Placa.Valor)
            .Select(grupo => grupo.MaxBy(evento => evento.FechaHora)!)
            .Where(ultimo => ultimo.Tipo == TipoDeEvento.Ingreso)
            .Select(ultimo => new VehiculoAdentro(ultimo.Placa, ultimo.TipoDeVehiculo, ultimo.FechaHora))
            .ToList();

        return Task.FromResult(adentro);
    }
}
