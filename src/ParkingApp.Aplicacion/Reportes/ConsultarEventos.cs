using System.Globalization;

using ParkingApp.Aplicacion.Configuracion;
using ParkingApp.Dominio.Accesos;

namespace ParkingApp.Aplicacion.Reportes;

/// <summary>
/// Caso de uso de las consultas del administrador (F-09): valida los filtros, normaliza la placa, convierte las fechas
/// con la zona horaria de la sede y arma los resultados que muestra el portal.
/// </summary>
public class ConsultarEventos : IServicioConsultas
{
    /// <summary>
    /// Mensaje cuando la fecha inicial del filtro es posterior a la final.
    /// </summary>
    public const string FechasInvalidas = "La fecha inicial debe ser anterior a la final";

    private readonly IRepositorioConsultas _repositorio;
    private readonly ISedeActual _sedeActual;

    /// <summary>
    /// Crea el caso de uso con sus dependencias.
    /// </summary>
    /// <param name="repositorio">Donde se consultan los eventos y los cobros.</param>
    /// <param name="sedeActual">Sede donde corre el portal; da la zona horaria.</param>
    public ConsultarEventos(IRepositorioConsultas repositorio, ISedeActual sedeActual)
    {
        _repositorio = repositorio;
        _sedeActual = sedeActual;
    }

    /// <inheritdoc/>
    public async Task<List<EventoAccesoDto>> ConsultarAccesosAsync(FiltrosConsultaDto filtros, CancellationToken cancelacion = default)
    {
        // 1. Se valida el rango de fechas.
        ValidarFechas(filtros);

        // 2. Las fechas del filtro son días en la hora de la sede. Los eventos se guardan en UTC, así que se convierten.
        DateTimeOffset? inicioUtc = null;
        DateTimeOffset? finUtc = null;

        if (filtros.FechaInicio.HasValue)
        {
            inicioUtc = InicioDelDiaEnUtc(filtros.FechaInicio.Value);
        }

        if (filtros.FechaFin.HasValue)
        {
            // El día final se incluye completo: el límite es el inicio del día siguiente, sin incluirlo.
            finUtc = InicioDelDiaEnUtc(filtros.FechaFin.Value.AddDays(1));
        }

        // 3. Se consulta con la placa normalizada.
        var eventos = await _repositorio.ConsultarAccesosAsync(inicioUtc, finUtc, NormalizarPlaca(filtros.Placa), cancelacion);

        // 4. Se arma el resultado con la hora de la sede.
        var resultado = new List<EventoAccesoDto>();

        foreach (var evento in eventos)
        {
            resultado.Add(new EventoAccesoDto
            {
                Placa = evento.Placa.Valor,
                TipoVehiculo = evento.TipoDeVehiculo.ToString(),
                TipoEvento = evento.Tipo.ToString(),
                FechaHora = TimeZoneInfo.ConvertTime(evento.FechaHora, _sedeActual.ZonaHoraria).DateTime,
                Carril = evento.Carril.ToString(CultureInfo.InvariantCulture),
                EsLecturaManual = evento.EsLecturaManual,
                ValidadoPor = evento.ValidadoPor,
            });
        }

        return resultado;
    }

    /// <inheritdoc/>
    public async Task<List<CobroConsultaDto>> ConsultarCobrosAsync(FiltrosConsultaDto filtros, CancellationToken cancelacion = default)
    {
        // 1. Se valida el rango de fechas.
        ValidarFechas(filtros);

        // 2. Los cobros se filtran por día operativo, que ya es una fecha de la sede: no hace falta convertir a UTC.
        DateOnly? diaInicio = null;
        DateOnly? diaFin = null;

        if (filtros.FechaInicio.HasValue)
        {
            diaInicio = DateOnly.FromDateTime(filtros.FechaInicio.Value);
        }

        if (filtros.FechaFin.HasValue)
        {
            diaFin = DateOnly.FromDateTime(filtros.FechaFin.Value);
        }

        // 3. Se consulta con la placa normalizada y el estado elegido.
        var cobros = await _repositorio.ConsultarCobrosAsync(diaInicio, diaFin, NormalizarPlaca(filtros.Placa), filtros.EstadoCobro, cancelacion);

        // 4. Se arma el resultado.
        var resultado = new List<CobroConsultaDto>();

        foreach (var cobro in cobros)
        {
            resultado.Add(new CobroConsultaDto
            {
                Placa = cobro.Placa.Valor,
                DiaOperativo = cobro.DiaOperativo,
                Franja = cobro.Franja,
                Valor = cobro.Valor,
                Estado = cobro.Estado,
            });
        }

        return resultado;
    }

    // La fecha inicial puede ser igual a la final (un solo día), pero no posterior.
    // El mensaje va sin el nombre del parámetro porque la pantalla lo muestra tal cual.
    private static void ValidarFechas(FiltrosConsultaDto filtros)
    {
        if (filtros.FechaInicio.HasValue && filtros.FechaFin.HasValue && filtros.FechaInicio.Value.Date > filtros.FechaFin.Value.Date)
        {
            throw new ArgumentException(FechasInvalidas);
        }
    }

    // Sin placa no se filtra. Con placa, se busca como se guarda: en mayúsculas, sin espacios ni guiones.
    private static string? NormalizarPlaca(string? placa)
    {
        if (string.IsNullOrWhiteSpace(placa))
        {
            return null;
        }

        return Placa.Normalizar(placa);
    }

    // Convierte las 00:00 de una fecha, en la hora de la sede, a UTC.
    private DateTimeOffset InicioDelDiaEnUtc(DateTime fecha)
    {
        var medianocheLocal = DateTime.SpecifyKind(fecha.Date, DateTimeKind.Unspecified);
        return new DateTimeOffset(TimeZoneInfo.ConvertTimeToUtc(medianocheLocal, _sedeActual.ZonaHoraria));
    }
}
