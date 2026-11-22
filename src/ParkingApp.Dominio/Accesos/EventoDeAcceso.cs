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

    /// <summary>
    /// Largo máximo del operador que valida una lectura: el mismo del nombre de usuario en la sesión.
    /// </summary>
    public const int LargoMaximoOperador = 256;

    private EventoDeAcceso(TipoDeEvento tipo, TipoDeVehiculo tipoDeVehiculo, Placa placa, Guid sedeId, int carril, int confianza, DateTimeOffset fechaHora, string? validadoPor)
    {
        Id = Guid.CreateVersion7();
        Tipo = tipo;
        TipoDeVehiculo = tipoDeVehiculo;
        Placa = placa;
        SedeId = sedeId;
        Carril = carril;
        Confianza = confianza;
        FechaHora = fechaHora;
        ValidadoPor = validadoPor;
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
    /// Tipo de vehículo, según el formato de su placa o, si no se reconoce, según lo que indicó el operador.
    /// </summary>
    public TipoDeVehiculo TipoDeVehiculo { get; private set; }

    /// <summary>
    /// Placa leída, o la que validó el operador, ya normalizada.
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
    /// Operador que validó la placa en la caja, o nada si la lectura fue automática (F-06).
    /// </summary>
    public string? ValidadoPor { get; private set; }

    /// <summary>
    /// Si el evento es una "lectura manual": un operador validó la placa.
    /// </summary>
    public bool EsLecturaManual => ValidadoPor != null;

    /// <summary>
    /// Registra el ingreso de un vehículo por un carril de entrada.
    /// </summary>
    /// <param name="placa">Placa leída.</param>
    /// <param name="tipoDeVehiculo">Tipo de vehículo: el del formato de la placa o el que indicó el operador.</param>
    /// <param name="sedeId">Sede del carril.</param>
    /// <param name="carril">Número del carril, desde 1.</param>
    /// <param name="confianza">Confianza de la lectura, de 0 a 100.</param>
    /// <param name="fechaHora">Fecha y hora del ingreso, en UTC.</param>
    /// <param name="validadoPor">Operador que validó la placa, o nada si la lectura fue automática.</param>
    /// <returns>El evento de ingreso.</returns>
    /// <exception cref="ArgumentOutOfRangeException">El carril o la confianza están fuera de rango.</exception>
    /// <exception cref="ArgumentOutOfRangeException">El tipo de vehículo no se puede asignar a un ingreso nuevo.</exception>
    /// <exception cref="ArgumentException">La fecha y la hora no están en UTC, o el operador está vacío o es muy largo.</exception>
    public static EventoDeAcceso RegistrarIngreso(Placa placa, TipoDeVehiculo tipoDeVehiculo, Guid sedeId, int carril, int confianza, DateTimeOffset fechaHora, string? validadoPor = null)
    {
        ArgumentNullException.ThrowIfNull(placa);

        if (tipoDeVehiculo is not TipoDeVehiculo.Carro and not TipoDeVehiculo.Moto)
        {
            throw new ArgumentOutOfRangeException(nameof(tipoDeVehiculo), tipoDeVehiculo, "El ingreso necesita un tipo de vehículo reconocido");
        }

        ValidarLectura(carril, confianza, fechaHora);
        var operador = validadoPor is null ? null : RevisarOperador(validadoPor);

        return new EventoDeAcceso(TipoDeEvento.Ingreso, tipoDeVehiculo, placa, sedeId, carril, confianza, fechaHora, operador);
    }

    /// <summary>
    /// Registra la salida de un vehículo por un carril de salida. A diferencia del ingreso, acepta un vehículo
    /// <see cref="TipoDeVehiculo.NoClasificado"/>, porque pudo entrar por ingreso manual con una placa sin formato reconocido.
    /// </summary>
    /// <param name="placa">Placa leída.</param>
    /// <param name="tipoDeVehiculo">Tipo de vehículo identificado por el formato de la placa, o no clasificado.</param>
    /// <param name="sedeId">Sede del carril.</param>
    /// <param name="carril">Número del carril, desde 1.</param>
    /// <param name="confianza">Confianza de la lectura, de 0 a 100.</param>
    /// <param name="fechaHora">Fecha y hora de la salida, en UTC.</param>
    /// <param name="validadoPor">Operador que validó la placa, o nada si la lectura fue automática.</param>
    /// <returns>El evento de salida.</returns>
    /// <exception cref="ArgumentOutOfRangeException">El carril o la confianza están fuera de rango.</exception>
    /// <exception cref="ArgumentException">La fecha y la hora no están en UTC, o el operador está vacío o es muy largo.</exception>
    public static EventoDeAcceso RegistrarSalida(Placa placa, TipoDeVehiculo tipoDeVehiculo, Guid sedeId, int carril, int confianza, DateTimeOffset fechaHora, string? validadoPor = null)
    {
        ArgumentNullException.ThrowIfNull(placa);

        ValidarLectura(carril, confianza, fechaHora);
        var operador = validadoPor is null ? null : RevisarOperador(validadoPor);

        return new EventoDeAcceso(TipoDeEvento.Salida, tipoDeVehiculo, placa, sedeId, carril, confianza, fechaHora, operador);
    }

    // Regla común de quien valida una lectura. Devuelve el operador sin espacios en los extremos.
    internal static string RevisarOperador(string operador)
    {
        if (string.IsNullOrWhiteSpace(operador))
        {
            throw new ArgumentException("Una lectura manual necesita el operador que la valida.", nameof(operador));
        }

        var operadorLimpio = operador.Trim();

        if (operadorLimpio.Length > LargoMaximoOperador)
        {
            throw new ArgumentException($"El operador no puede tener más de {LargoMaximoOperador} caracteres.", nameof(operador));
        }

        return operadorLimpio;
    }

    // Reglas comunes del ingreso, la salida y la lectura pendiente: carril desde 1, confianza de 0 a 100 y hora en UTC.
    internal static void ValidarLectura(int carril, int confianza, DateTimeOffset fechaHora)
    {
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
    }
}
