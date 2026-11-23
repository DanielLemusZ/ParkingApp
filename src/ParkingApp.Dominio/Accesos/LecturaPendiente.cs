namespace ParkingApp.Dominio.Accesos;

/// <summary>
/// Lectura de un carril que no abrió la barrera porque el operador debe validar su placa (F-06).
/// Guarda lo que se leyó y, al validarse, lo que confirmó el operador.
/// </summary>
public sealed class LecturaPendiente
{
    private LecturaPendiente(TipoDeEvento tipo, MotivoDeLecturaPendiente motivo, Placa placaLeida, Guid sedeId, int carril, int confianza, DateTimeOffset fechaHora)
    {
        Id = Guid.CreateVersion7();
        Tipo = tipo;
        Motivo = motivo;
        PlacaLeida = placaLeida;
        SedeId = sedeId;
        Carril = carril;
        Confianza = confianza;
        FechaHora = fechaHora;
        Estado = EstadoDeLectura.Pendiente;
    }

    // Permite reconstruir una lectura ya guardada.
    private LecturaPendiente()
    {
    }

    /// <summary>
    /// Identificador de la lectura.
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// Si la lectura llegó por un carril de entrada (ingreso) o de salida.
    /// </summary>
    public TipoDeEvento Tipo { get; private set; }

    /// <summary>
    /// Por qué la lectura necesita validación.
    /// </summary>
    public MotivoDeLecturaPendiente Motivo { get; private set; }

    /// <summary>
    /// Placa como se leyó, ya normalizada. Puede traer caracteres que no son letras ni números.
    /// </summary>
    // "null!" le indica al compilador que este valor nunca queda nulo: lo asigna el constructor
    // o, al leer una lectura guardada, la base de datos.
    public Placa PlacaLeida { get; private set; } = null!;

    /// <summary>
    /// Sede del carril.
    /// </summary>
    public Guid SedeId { get; private set; }

    /// <summary>
    /// Número del carril, desde 1.
    /// </summary>
    public int Carril { get; private set; }

    /// <summary>
    /// Confianza de la lectura, de 0 a 100.
    /// </summary>
    public int Confianza { get; private set; }

    /// <summary>
    /// Fecha y hora de la lectura, en UTC.
    /// </summary>
    public DateTimeOffset FechaHora { get; private set; }

    /// <summary>
    /// Si sigue pendiente o ya se validó.
    /// </summary>
    public EstadoDeLectura Estado { get; private set; }

    /// <summary>
    /// Placa que confirmó o digitó el operador, o nada si sigue pendiente.
    /// </summary>
    public Placa? PlacaValidada { get; private set; }

    /// <summary>
    /// Operador que validó la lectura, o nada si sigue pendiente.
    /// </summary>
    public string? ValidadaPor { get; private set; }

    /// <summary>
    /// Fecha y hora de la validación en UTC, o nada si sigue pendiente.
    /// </summary>
    public DateTimeOffset? FechaDeValidacion { get; private set; }

    /// <summary>
    /// Lo que respondió el carril después de validar, por ejemplo <c>LLENO: carros</c>.
    /// </summary>
    public string? Resultado { get; private set; }

    /// <summary>
    /// Deja una lectura pendiente de validación.
    /// </summary>
    /// <param name="tipo">Si llegó por un carril de entrada o de salida.</param>
    /// <param name="motivo">Por qué necesita validación.</param>
    /// <param name="placaLeida">Placa como se leyó.</param>
    /// <param name="sedeId">Sede del carril.</param>
    /// <param name="carril">Número del carril, desde 1.</param>
    /// <param name="confianza">Confianza de la lectura, de 0 a 100.</param>
    /// <param name="fechaHora">Fecha y hora de la lectura, en UTC.</param>
    /// <returns>La lectura pendiente.</returns>
    /// <exception cref="ArgumentOutOfRangeException">El carril o la confianza están fuera de rango.</exception>
    /// <exception cref="ArgumentException">
    /// El formato no reconocido se usó en una salida, "no figura adentro" se usó en un ingreso, o la fecha y la hora no están en UTC.
    /// </exception>
    public static LecturaPendiente Registrar(
        TipoDeEvento tipo,
        MotivoDeLecturaPendiente motivo,
        Placa placaLeida,
        Guid sedeId,
        int carril,
        int confianza,
        DateTimeOffset fechaHora)
    {
        ArgumentNullException.ThrowIfNull(placaLeida);

        if (tipo == TipoDeEvento.Salida && motivo == MotivoDeLecturaPendiente.FormatoNoReconocido)
        {
            throw new ArgumentException("La salida no exige un formato de placa reconocido.", nameof(motivo));
        }

        if (tipo == TipoDeEvento.Ingreso && motivo == MotivoDeLecturaPendiente.NoFiguraAdentro)
        {
            throw new ArgumentException("Solo la salida revisa si la placa figura adentro.", nameof(motivo));
        }

        EventoDeAcceso.ValidarLectura(carril, confianza, fechaHora);

        return new LecturaPendiente(tipo, motivo, placaLeida, sedeId, carril, confianza, fechaHora);
    }

    /// <summary>
    /// Valida la lectura: pasa de Pendiente a Validada con la placa y el operador. Es la única transición válida.
    /// </summary>
    /// <param name="placa">Placa que confirmó o digitó el operador.</param>
    /// <param name="operador">Operador que la valida.</param>
    /// <param name="fechaHora">Fecha y hora de la validación, en UTC.</param>
    /// <exception cref="InvalidOperationException">La lectura ya fue validada.</exception>
    /// <exception cref="ArgumentException">
    /// La placa tiene algo distinto de letras y números, el operador está vacío o es muy largo, o la fecha y la hora no están en UTC.
    /// </exception>
    public void Validar(Placa placa, string operador, DateTimeOffset fechaHora)
    {
        ArgumentNullException.ThrowIfNull(placa);

        if (Estado != EstadoDeLectura.Pendiente)
        {
            throw new InvalidOperationException("Solo se puede validar una lectura pendiente.");
        }

        if (!Placa.SoloTieneLetrasYNumeros(placa.Valor))
        {
            throw new ArgumentException("Una placa validada solo admite letras y números.", nameof(placa));
        }

        if (fechaHora.Offset != TimeSpan.Zero)
        {
            throw new ArgumentException("La fecha y la hora deben estar en UTC", nameof(fechaHora));
        }

        ValidadaPor = EventoDeAcceso.RevisarOperador(operador);
        PlacaValidada = placa;
        FechaDeValidacion = fechaHora;
        Estado = EstadoDeLectura.Validada;
    }

    /// <summary>
    /// Anota lo que respondió el carril después de validar la lectura.
    /// </summary>
    /// <param name="resultado">Motivo que vio el operador.</param>
    /// <exception cref="InvalidOperationException">La lectura todavía no está validada.</exception>
    public void AnotarResultado(string resultado)
    {
        if (Estado != EstadoDeLectura.Validada)
        {
            throw new InvalidOperationException("Solo una lectura validada tiene resultado.");
        }

        Resultado = resultado;
    }
}
