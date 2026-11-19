namespace ParkingApp.Dominio.Cobros;

/// <summary>
/// Cobro generado por la estancia de un vehículo en el parqueadero.
/// </summary>
public sealed class Cobro
{
    private Cobro()
    {
    }

    /// <summary>
    /// Crea un nuevo cobro asociado a una placa.
    /// </summary>
    /// <param name="placa">Placa del vehículo.</param>
    /// <param name="valor">Valor monetario del cobro.</param>
    /// <param name="estado">Estado inicial del cobro.</param>
    /// <param name="fechaCreacion">Fecha y hora de creación en UTC.</param>
    public Cobro(string placa, decimal valor, EstadoCobro estado, DateTimeOffset fechaCreacion)
    {
        Id = Guid.CreateVersion7();
        Placa = placa;
        Valor = valor;
        Estado = estado;
        FechaCreacion = fechaCreacion;
    }

    /// <summary>
    /// Identificador único del cobro.
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// Placa normalizada del vehículo asociado al cobro.
    /// </summary>
    public string Placa { get; private set; } = string.Empty;

    /// <summary>
    /// Valor total del cobro.
    /// </summary>
    public decimal Valor { get; private set; }

    /// <summary>
    /// Estado actual del cobro.
    /// </summary>
    public EstadoCobro Estado { get; private set; }

    /// <summary>
    /// Fecha y hora de generación del cobro en UTC.
    /// </summary>
    public DateTimeOffset FechaCreacion { get; private set; }
}
