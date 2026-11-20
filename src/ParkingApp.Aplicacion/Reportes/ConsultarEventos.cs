using System.Globalization;
using ParkingApp.Dominio.Accesos;
using ParkingApp.Aplicacion.Configuracion;

namespace ParkingApp.Aplicacion.Reportes;

/// <summary>
/// Caso de uso para consultar los reportes validando reglas de negocio.
/// </summary>
public class ConsultarEventos : IServicioConsultas
{
    private readonly IRepositorioConsultas _repositorio;
    private readonly ISedeActual _sedeActual;

    public ConsultarEventos(IRepositorioConsultas repositorio, ISedeActual sedeActual)
    {
        _repositorio = repositorio;
        _sedeActual = sedeActual;
    }

    /// <inheritdoc/>
    public async Task<List<EventoAccesoDto>> ConsultarAccesosAsync(FiltrosConsultaDto filtros)
    {
        // 1. Regla de negocio: Validar que el inicio no sea mayor al fin
        if (filtros.FechaInicio.HasValue && filtros.FechaFin.HasValue && filtros.FechaInicio > filtros.FechaFin)
        {
            throw new ArgumentException("La fecha de inicio no puede ser posterior a la fecha de fin.");
        }

        var zonaHoraria = _sedeActual.ZonaHoraria.BaseUtcOffset;
        DateTimeOffset? inicioUtc = null;
        DateTimeOffset? finUtc = null;

        // 2. Convertir las fechas a UTC basándose en la zona horaria de la sede actual
        if (filtros.FechaInicio.HasValue)
        {
            inicioUtc = new DateTimeOffset(filtros.FechaInicio.Value.Date, zonaHoraria).ToUniversalTime();
        }

        if (filtros.FechaFin.HasValue)
        {
            // El fin del día es el inicio del día siguiente (exclusivo)
            finUtc = new DateTimeOffset(filtros.FechaFin.Value.Date.AddDays(1), zonaHoraria).ToUniversalTime();
        }

        // 3. Normalizar la placa antes de ir a la BD
        string? placaNormalizada = null;
        if (!string.IsNullOrWhiteSpace(filtros.Placa))
        {
            placaNormalizada = Placa.Normalizar(filtros.Placa);
        }

        // 4. Buscar en infraestructura enviando parámetros limpios y listos
        var eventosDb = await _repositorio.ConsultarAccesosAsync(inicioUtc, finUtc, placaNormalizada);

        // 5. Mapear el resultado y devolver la fecha convertida a la hora local de la sede
        return eventosDb.Select(a => new EventoAccesoDto
        {
            Placa = a.Placa.Valor,
            TipoVehiculo = a.TipoDeVehiculo.ToString(),
            TipoEvento = a.Tipo.ToString(),
            FechaHora = a.FechaHora.ToOffset(zonaHoraria).DateTime,
            Carril = a.Carril.ToString(CultureInfo.InvariantCulture),
            EsLecturaManual = false
        }).ToList();
    }

    /// <inheritdoc/>
    public Task<List<CobroConsultaDto>> ConsultarCobrosAsync(FiltrosConsultaDto filtros)
    {
        // Como indicó Daniel, los cobros se integrarán en otro PR más adelante.
        return Task.FromResult(new List<CobroConsultaDto>());
    }
}