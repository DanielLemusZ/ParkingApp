using ParkingApp.Dominio.Configuracion;

namespace ParkingApp.Aplicacion.Configuracion;

/// <summary>
/// Ubica un instante en el día operativo y la franja de la sede. Lo usan la cadena del carril y los casos de uso,
/// para que todos calculen el día y la franja de la misma forma.
/// </summary>
public sealed class CalendarioOperativo
{
    private readonly ISedeActual sede;
    private readonly IHorarioDeOperacionActual horario;

    /// <summary>
    /// Crea el calendario con la sede y su horario.
    /// </summary>
    /// <param name="sede">Sede cuya zona horaria se usa.</param>
    /// <param name="horario">Horario de operación vigente.</param>
    public CalendarioOperativo(ISedeActual sede, IHorarioDeOperacionActual horario)
    {
        this.sede = sede;
        this.horario = horario;
    }

    /// <summary>
    /// Convierte un instante en UTC a la hora de la sede y calcula su día operativo y su franja.
    /// </summary>
    /// <param name="fechaHoraUtc">Instante en UTC.</param>
    /// <returns>La hora local, el día operativo y la franja.</returns>
    public MomentoOperativo Ubicar(DateTimeOffset fechaHoraUtc)
    {
        var horaLocal = TimeZoneInfo.ConvertTime(fechaHoraUtc, sede.ZonaHoraria);
        var horarioActual = horario.Horario;

        return new MomentoOperativo(horaLocal, horarioActual.DiaOperativo(horaLocal), horarioActual.Franja(horaLocal));
    }
}

/// <summary>
/// Un instante visto desde la sede.
/// </summary>
/// <param name="HoraLocal">Fecha y hora en la zona horaria de la sede.</param>
/// <param name="DiaOperativo">Día operativo al que pertenece.</param>
/// <param name="Franja">Franja tarifaria de esa hora.</param>
public sealed record MomentoOperativo(DateTimeOffset HoraLocal, DateOnly DiaOperativo, FranjaTarifaria Franja);
