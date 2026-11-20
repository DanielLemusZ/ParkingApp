using ParkingApp.Aplicacion.Reportes;
using ParkingApp.Dominio.Accesos;
using ParkingApp.Dominio.Cobros;

namespace ParkingApp.Pruebas.Dobles;

/// <summary>
/// Repositorio de consultas para las pruebas. Guarda los filtros que recibe el caso de uso y devuelve los eventos
/// y cobros que cada prueba prepara. El filtrado real lo hace la base de datos.
/// </summary>
internal sealed class RepositorioConsultasDePrueba : IRepositorioConsultas
{
    public List<EventoDeAcceso> Eventos { get; } = new List<EventoDeAcceso>();

    public List<Cobro> Cobros { get; } = new List<Cobro>();

    public DateTimeOffset? InicioUtcRecibido { get; private set; }

    public DateTimeOffset? FinUtcRecibido { get; private set; }

    public DateOnly? DiaInicioRecibido { get; private set; }

    public DateOnly? DiaFinRecibido { get; private set; }

    public string? PlacaRecibida { get; private set; }

    public EstadoDeCobro? EstadoRecibido { get; private set; }

    public bool FueConsultado { get; private set; }

    public Task<List<EventoDeAcceso>> ConsultarAccesosAsync(DateTimeOffset? fechaInicioUtc, DateTimeOffset? fechaFinUtc, string? placaNormalizada, CancellationToken cancelacion = default)
    {
        FueConsultado = true;
        InicioUtcRecibido = fechaInicioUtc;
        FinUtcRecibido = fechaFinUtc;
        PlacaRecibida = placaNormalizada;
        return Task.FromResult(Eventos);
    }

    public Task<List<Cobro>> ConsultarCobrosAsync(DateOnly? diaInicio, DateOnly? diaFin, string? placaNormalizada, EstadoDeCobro? estado, CancellationToken cancelacion = default)
    {
        FueConsultado = true;
        DiaInicioRecibido = diaInicio;
        DiaFinRecibido = diaFin;
        PlacaRecibida = placaNormalizada;
        EstadoRecibido = estado;
        return Task.FromResult(Cobros);
    }
}
