namespace ParkingApp.Dominio.Configuracion;

/// <summary>
/// Horario de la sede: cuándo empieza el día operativo y cuándo empieza la franja nocturna.
/// Es el único lugar donde se calculan el día operativo y la franja de una hora local.
/// </summary>
public sealed record HorarioDeOperacion
{
    /// <summary>
    /// Crea el horario de una sede.
    /// </summary>
    /// <param name="inicioDelDia">Hora en que empieza el día operativo y la franja diurna, por ejemplo 06:00.</param>
    /// <param name="inicioFranjaNocturna">Hora en que empieza la franja nocturna, por ejemplo 18:00.</param>
    /// <exception cref="ArgumentException">Las dos horas son iguales.</exception>
    public HorarioDeOperacion(TimeOnly inicioDelDia, TimeOnly inicioFranjaNocturna)
    {
        if (inicioDelDia == inicioFranjaNocturna)
        {
            throw new ArgumentException("La franja nocturna no puede empezar a la misma hora que el día operativo.", nameof(inicioFranjaNocturna));
        }

        InicioDelDia = inicioDelDia;
        InicioFranjaNocturna = inicioFranjaNocturna;
    }

    /// <summary>
    /// Horario inicial del MVP: el día operativo y la franja diurna empiezan a las 06:00, y la nocturna a las 18:00.
    /// </summary>
    public static HorarioDeOperacion Inicial { get; } = new HorarioDeOperacion(new TimeOnly(6, 0), new TimeOnly(18, 0));

    /// <summary>
    /// Hora en que empieza el día operativo y la franja diurna.
    /// </summary>
    public TimeOnly InicioDelDia { get; }

    /// <summary>
    /// Hora en que empieza la franja nocturna.
    /// </summary>
    public TimeOnly InicioFranjaNocturna { get; }

    /// <summary>
    /// Calcula el día operativo de una hora local de la sede. Antes del inicio del día, la hora pertenece al día anterior.
    /// </summary>
    /// <param name="horaLocal">Fecha y hora en la zona horaria de la sede.</param>
    /// <returns>El día operativo, por ejemplo el miércoles para un jueves a las 05:59.</returns>
    public DateOnly DiaOperativo(DateTimeOffset horaLocal)
    {
        var fecha = DateOnly.FromDateTime(horaLocal.DateTime);
        var hora = TimeOnly.FromDateTime(horaLocal.DateTime);

        return hora < InicioDelDia ? fecha.AddDays(-1) : fecha;
    }

    /// <summary>
    /// Calcula la franja de una hora local de la sede: diurna desde el inicio del día hasta antes del inicio de la nocturna.
    /// </summary>
    /// <param name="horaLocal">Fecha y hora en la zona horaria de la sede.</param>
    /// <returns>La franja diurna o nocturna.</returns>
    public FranjaTarifaria Franja(DateTimeOffset horaLocal)
    {
        var hora = TimeOnly.FromDateTime(horaLocal.DateTime);

        // IsBetween incluye el inicio y excluye el final: 06:00 es diurna y 18:00 es nocturna.
        return hora.IsBetween(InicioDelDia, InicioFranjaNocturna) ? FranjaTarifaria.Diurna : FranjaTarifaria.Nocturna;
    }
}
