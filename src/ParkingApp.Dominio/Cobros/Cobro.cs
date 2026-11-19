using ParkingApp.Dominio.Accesos;
using ParkingApp.Dominio.Configuracion;

namespace ParkingApp.Dominio.Cobros;

/// <summary>
/// Cobro de un vehículo por un día operativo en una sede. Guarda la tarifa con la que se calculó.
/// </summary>
public sealed class Cobro
{
    private Cobro(Placa placa, TipoDeVehiculo tipoDeVehiculo, Guid sedeId, DateOnly diaOperativo, Tarifa tarifa, DateTimeOffset fechaHora)
    {
        Id = Guid.CreateVersion7();
        Placa = placa;
        TipoDeVehiculo = tipoDeVehiculo;
        SedeId = sedeId;
        DiaOperativo = diaOperativo;
        Franja = tarifa.Franja;
        TarifaId = tarifa.Id;
        Valor = tarifa.Valor;
        Estado = EstadoDeCobro.Pendiente;
        FechaHora = fechaHora;
    }

    // Permite reconstruir un cobro ya guardado.
    private Cobro()
    {
    }

    /// <summary>
    /// Identificador del cobro.
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// Placa del vehículo, ya normalizada.
    /// </summary>
    // "null!" le indica al compilador que este valor nunca queda nulo: lo asigna el constructor
    // o, al leer un cobro guardado, la base de datos.
    public Placa Placa { get; private set; } = null!;

    /// <summary>
    /// Tipo de vehículo cobrado.
    /// </summary>
    public TipoDeVehiculo TipoDeVehiculo { get; private set; }

    /// <summary>
    /// Sede donde se generó el cobro.
    /// </summary>
    public Guid SedeId { get; private set; }

    /// <summary>
    /// Día operativo que cubre el cobro (de 06:00 a 05:59 del día siguiente, en la hora de la sede).
    /// </summary>
    public DateOnly DiaOperativo { get; private set; }

    /// <summary>
    /// Franja del primer ingreso del día, que definió la tarifa.
    /// </summary>
    public FranjaTarifaria Franja { get; private set; }

    /// <summary>
    /// Versión de la tarifa con la que se calculó el cobro.
    /// </summary>
    public Guid TarifaId { get; private set; }

    /// <summary>
    /// Valor del cobro en pesos colombianos. No cambia si después cambia la tarifa.
    /// </summary>
    public decimal Valor { get; private set; }

    /// <summary>
    /// Estado del cobro.
    /// </summary>
    public EstadoDeCobro Estado { get; private set; }

    /// <summary>
    /// Fecha y hora en que se generó el cobro, en UTC.
    /// </summary>
    public DateTimeOffset FechaHora { get; private set; }

    /// <summary>
    /// Genera el cobro pendiente del primer ingreso de un vehículo en el día operativo.
    /// </summary>
    /// <param name="placa">Placa del vehículo.</param>
    /// <param name="tipoDeVehiculo">Tipo de vehículo, que debe coincidir con el de la tarifa.</param>
    /// <param name="sedeId">Sede del ingreso.</param>
    /// <param name="diaOperativo">Día operativo del ingreso.</param>
    /// <param name="tarifa">Tarifa vigente para el tipo de vehículo y la franja del ingreso.</param>
    /// <param name="fechaHora">Fecha y hora del ingreso, en UTC.</param>
    /// <returns>El cobro en estado pendiente, con el valor, la franja y la versión de la tarifa.</returns>
    /// <exception cref="ArgumentException">
    /// La tarifa no genera cobro, no corresponde al tipo de vehículo, no está vigente en el día operativo,
    /// o la fecha y la hora no están en UTC.
    /// </exception>
    public static Cobro Generar(Placa placa, TipoDeVehiculo tipoDeVehiculo, Guid sedeId, DateOnly diaOperativo, Tarifa tarifa, DateTimeOffset fechaHora)
    {
        ArgumentNullException.ThrowIfNull(placa);
        ArgumentNullException.ThrowIfNull(tarifa);

        if (!tarifa.GeneraCobro)
        {
            throw new ArgumentException("Una tarifa de $0 no genera cobro.", nameof(tarifa));
        }

        if (tarifa.TipoDeVehiculo != tipoDeVehiculo)
        {
            throw new ArgumentException("La tarifa no corresponde al tipo de vehículo.", nameof(tarifa));
        }

        if (!tarifa.EstaVigenteEn(diaOperativo))
        {
            throw new ArgumentException("La tarifa no está vigente en el día operativo.", nameof(tarifa));
        }

        if (fechaHora.Offset != TimeSpan.Zero)
        {
            throw new ArgumentException("La fecha y la hora deben estar en UTC", nameof(fechaHora));
        }

        return new Cobro(placa, tipoDeVehiculo, sedeId, diaOperativo, tarifa, fechaHora);
    }
}
