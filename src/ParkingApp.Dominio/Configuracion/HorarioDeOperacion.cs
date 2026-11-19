namespace ParkingApp.Dominio.Configuracion;

/// <summary>
/// Horario de la sede: cuándo empiezan la franja diurna y la nocturna. El día operativo siempre empieza a las 06:00 (S-02).
/// Es el único lugar donde se calculan el día operativo y la franja de una hora local.
/// </summary>
public sealed record HorarioDeOperacion
{
    /// <summary>
    /// Hora en que empieza el día operativo, de 06:00 a 05:59 del día siguiente (S-02). No es configurable.
    /// </summary>
    public static readonly TimeOnly InicioDelDiaOperativo = new TimeOnly(6, 0);

    /// <summary>
    /// Crea el horario de una sede.
    /// </summary>
    /// <param name="inicioFranjaDiurna">Hora en que empieza la franja diurna, por ejemplo 06:00.</param>
    /// <param name="inicioFranjaNocturna">Hora en que empieza la franja nocturna, por ejemplo 18:00.</param>
    /// <exception cref="ArgumentException">Las dos franjas empiezan a la misma hora.</exception>
    public HorarioDeOperacion(TimeOnly inicioFranjaDiurna, TimeOnly inicioFranjaNocturna)
    {
        if (inicioFranjaDiurna == inicioFranjaNocturna)
        {
            throw new ArgumentException("Las franjas no pueden empezar a la misma hora.", nameof(inicioFranjaNocturna));
        }

        InicioFranjaDiurna = inicioFranjaDiurna;
        InicioFranjaNocturna = inicioFranjaNocturna;
    }

    /// <summary>
    /// Horario inicial del MVP: la franja diurna empieza a las 06:00 y la nocturna a las 18:00.
    /// </summary>
    public static HorarioDeOperacion Inicial { get; } = new HorarioDeOperacion(new TimeOnly(6, 0), new TimeOnly(18, 0));

    /// <summary>
    /// Hora en que empieza la franja diurna.
    /// </summary>
    public TimeOnly InicioFranjaDiurna { get; }

    /// <summary>
    /// Hora en que empieza la franja nocturna.
    /// </summary>
    public TimeOnly InicioFranjaNocturna { get; }

    /// <summary>
    /// Calcula el día operativo de una hora local de la sede. Antes de las 06:00, la hora pertenece al día anterior.
    /// No depende de las franjas: es igual en cualquier horario.
    /// </summary>
    /// <param name="horaLocal">Fecha y hora en la zona horaria de la sede.</param>
    /// <returns>El día operativo, por ejemplo el miércoles para un jueves a las 05:59.</returns>
    public static DateOnly DiaOperativo(DateTimeOffset horaLocal)
    {
        var fecha = DateOnly.FromDateTime(horaLocal.DateTime);
        var hora = TimeOnly.FromDateTime(horaLocal.DateTime);

        return hora < InicioDelDiaOperativo ? fecha.AddDays(-1) : fecha;
    }

    /// <summary>
    /// Calcula la franja de una hora local de la sede: diurna desde su inicio hasta antes del inicio de la nocturna.
    /// </summary>
    /// <param name="horaLocal">Fecha y hora en la zona horaria de la sede.</param>
    /// <returns>La franja diurna o nocturna.</returns>
    public FranjaTarifaria Franja(DateTimeOffset horaLocal)
    {
        var hora = TimeOnly.FromDateTime(horaLocal.DateTime);

        // IsBetween incluye el inicio y excluye el final: 06:00 es diurna y 18:00 es nocturna.
        return hora.IsBetween(InicioFranjaDiurna, InicioFranjaNocturna) ? FranjaTarifaria.Diurna : FranjaTarifaria.Nocturna;
    }
}
