namespace ParkingApp.Dominio.Accesos;

/// <summary>
/// Paso de un vehículo por un carril de la sede: un ingreso o una salida.
/// </summary>
public sealed class EventoDeAcceso
{
    /// <summary>
    /// Confianza mínima de una lectura de placa.
    /// </summary>
    public const int ConfianzaMinima = 0;

    /// <summary>
    /// Confianza máxima de una lectura de placa.
    /// </summary>
    public const int ConfianzaMaxima = 100;

    private EventoDeAcceso(TipoDeEvento tipo, Placa placa, Guid sedeId, int carril, int confianza, DateTimeOffset fechaHora)
    {
        Id = Guid.CreateVersion7();
        Tipo = tipo;
        Placa = placa;
        SedeId = sedeId;
        Carril = carril;
        Confianza = confianza;
        FechaHora = fechaHora;
    }

    // Permite reconstruir un evento ya guardado.
    private EventoDeAcceso()
    {
    }

    /// <summary>
    /// Identificador del evento.
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// Si el vehículo entró o salió.
    /// </summary>
    public TipoDeEvento Tipo { get; private set; }

    /// <summary>
    /// Placa leída, ya normalizada.
    /// </summary>
    // "null!" le indica al compilador que este valor nunca queda nulo: lo asigna el constructor
    // o, al leer un evento guardado, la base de datos.
    public Placa Placa { get; private set; } = null!;

    /// <summary>
    /// Sede donde ocurrió el evento.
    /// </summary>
    public Guid SedeId { get; private set; }

    /// <summary>
    /// Número del carril, desde 1.
    /// </summary>
    public int Carril { get; private set; }

    /// <summary>
    /// Confianza de la lectura de la placa, de 0 a 100.
    /// </summary>
    public int Confianza { get; private set; }

    /// <summary>
    /// Fecha y hora del evento, en UTC.
    /// </summary>
    public DateTimeOffset FechaHora { get; private set; }

    /// <summary>
    /// Registra el ingreso de un vehículo por un carril de entrada.
    /// </summary>
    /// <param name="placa">Placa leída.</param>
    /// <param name="sedeId">Sede del carril.</param>
    /// <param name="carril">Número del carril, desde 1.</param>
    /// <param name="confianza">Confianza de la lectura, de 0 a 100.</param>
    /// <param name="fechaHora">Fecha y hora del ingreso, en UTC.</param>
    /// <returns>El evento de ingreso.</returns>
    /// <exception cref="ArgumentOutOfRangeException">El carril o la confianza están fuera de rango.</exception>
    /// <exception cref="ArgumentException">La fecha y la hora no están en UTC.</exception>
    public static EventoDeAcceso RegistrarIngreso(Placa placa, Guid sedeId, int carril, int confianza, DateTimeOffset fechaHora)
    {
        ArgumentNullException.ThrowIfNull(placa);

        if (carril < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(carril), carril, "El carril debe ser 1 o mayor");
        }

        if (confianza < ConfianzaMinima || confianza > ConfianzaMaxima)
        {
            throw new ArgumentOutOfRangeException(nameof(confianza), confianza, "La confianza debe estar entre 0 y 100");
        }

        if (fechaHora.Offset != TimeSpan.Zero)
        {
            throw new ArgumentException("La fecha y la hora deben estar en UTC", nameof(fechaHora));
        }

        return new EventoDeAcceso(TipoDeEvento.Ingreso, placa, sedeId, carril, confianza, fechaHora);
    }
}
