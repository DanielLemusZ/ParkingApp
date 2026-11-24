namespace ParkingApp.Dominio.Pagos;

/// <summary>
/// Pago de un cobro. Lo crea el propio cobro al pagarse (<c>Cobro.Pagar</c>), para que el pago y el cambio
/// de estado del cobro ocurran juntos.
/// </summary>
public sealed class Pago
{
    /// <summary>
    /// Largo máximo del nombre del operador que registra el pago.
    /// </summary>
    public const int LargoMaximoOperador = 100;

    /// <summary>
    /// Largo máximo de la transacción que devuelve la pasarela de pagos.
    /// </summary>
    public const int LargoMaximoTransaccion = 100;

    private Pago(
        Guid cobroId,
        decimal valor,
        MedioDePago medio,
        string operador,
        Guid referencia,
        DateTimeOffset fechaHora,
        string? transaccionDeLaPasarela)
    {
        Id = Guid.CreateVersion7();
        CobroId = cobroId;
        Valor = valor;
        Medio = medio;
        Operador = operador;
        Referencia = referencia;
        FechaHora = fechaHora;
        TransaccionDeLaPasarela = transaccionDeLaPasarela;
    }

    // Permite reconstruir un pago ya guardado.
    private Pago()
    {
    }

    /// <summary>
    /// Identificador del pago.
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// Cobro que se pagó.
    /// </summary>
    public Guid CobroId { get; private set; }

    /// <summary>
    /// Valor pagado en pesos colombianos; es el valor del cobro.
    /// </summary>
    public decimal Valor { get; private set; }

    /// <summary>
    /// Medio de pago.
    /// </summary>
    public MedioDePago Medio { get; private set; }

    /// <summary>
    /// Operador que registró el pago.
    /// </summary>
    // "null!" le indica al compilador que este valor nunca queda nulo: lo asigna el constructor
    // o, al leer un pago guardado, la base de datos.
    public string Operador { get; private set; } = null!;

    /// <summary>
    /// Referencia única de la solicitud de pago. Si la misma solicitud llega dos veces, trae la misma referencia
    /// y la base de datos rechaza el segundo pago (idempotencia).
    /// </summary>
    public Guid Referencia { get; private set; }

    /// <summary>
    /// Fecha y hora del pago, en UTC.
    /// </summary>
    public DateTimeOffset FechaHora { get; private set; }

    /// <summary>
    /// Transacción que devolvió la pasarela de pagos, o <see langword="null"/> si se pagó en efectivo. Cuando se paga
    /// todo lo pendiente de una placa, todos sus pagos llevan la misma transacción.
    /// </summary>
    public string? TransaccionDeLaPasarela { get; private set; }

    /// <summary>
    /// Crea el pago de un cobro. Solo lo llama el cobro, que antes comprobó que estaba pendiente.
    /// </summary>
    /// <param name="cobroId">Cobro que se paga.</param>
    /// <param name="valor">Valor del cobro.</param>
    /// <param name="medio">Medio de pago.</param>
    /// <param name="operador">Operador que registra el pago.</param>
    /// <param name="referencia">Referencia única de la solicitud de pago.</param>
    /// <param name="fechaHora">Fecha y hora del pago, en UTC.</param>
    /// <param name="transaccionDeLaPasarela">Transacción de la pasarela; solo con el medio <see cref="MedioDePago.Pasarela"/>.</param>
    /// <returns>El pago registrado.</returns>
    /// <exception cref="ArgumentException">
    /// El operador está vacío o es muy largo, la referencia está vacía, la fecha y la hora no están en UTC, o la
    /// transacción de la pasarela falta, sobra o es muy larga.
    /// </exception>
    internal static Pago Registrar(
        Guid cobroId,
        decimal valor,
        MedioDePago medio,
        string operador,
        Guid referencia,
        DateTimeOffset fechaHora,
        string? transaccionDeLaPasarela = null)
    {
        if (string.IsNullOrWhiteSpace(operador))
        {
            throw new ArgumentException("El pago necesita el operador que lo registra.", nameof(operador));
        }

        var operadorLimpio = operador.Trim();

        if (operadorLimpio.Length > LargoMaximoOperador)
        {
            throw new ArgumentException($"El operador no puede tener más de {LargoMaximoOperador} caracteres.", nameof(operador));
        }

        if (referencia == Guid.Empty)
        {
            throw new ArgumentException("El pago necesita una referencia.", nameof(referencia));
        }

        if (fechaHora.Offset != TimeSpan.Zero)
        {
            throw new ArgumentException("La fecha y la hora deben estar en UTC", nameof(fechaHora));
        }

        var transaccion = ValidarTransaccion(medio, transaccionDeLaPasarela);

        return new Pago(cobroId, valor, medio, operadorLimpio, referencia, fechaHora, transaccion);
    }

    // Con la pasarela, la transacción es la prueba de que el pago se aprobó; con el efectivo no hay transacción.
    private static string? ValidarTransaccion(MedioDePago medio, string? transaccionDeLaPasarela)
    {
        if (medio != MedioDePago.Pasarela)
        {
            if (transaccionDeLaPasarela is not null)
            {
                throw new ArgumentException("Solo un pago con la pasarela lleva transacción.", nameof(transaccionDeLaPasarela));
            }

            return null;
        }

        if (string.IsNullOrWhiteSpace(transaccionDeLaPasarela))
        {
            throw new ArgumentException("El pago con la pasarela necesita la transacción.", nameof(transaccionDeLaPasarela));
        }

        var transaccion = transaccionDeLaPasarela.Trim();

        if (transaccion.Length > LargoMaximoTransaccion)
        {
            throw new ArgumentException(
                $"La transacción no puede tener más de {LargoMaximoTransaccion} caracteres.",
                nameof(transaccionDeLaPasarela));
        }

        return transaccion;
    }
}
