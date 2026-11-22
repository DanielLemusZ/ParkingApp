using ParkingApp.Dominio.Accesos;
using ParkingApp.Dominio.Configuracion;
using ParkingApp.Dominio.Pagos;

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
    /// Franja del primer ingreso del día, o diurna si es el cobro de un día nuevo.
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

    /// <summary>
    /// Paga el cobro: pasa de Pendiente a Pagado y crea el pago con el valor del cobro. Es la única transición válida: un cobro Pagado no se vuelve a pagar (F-04).
    /// </summary>
    /// <param name="medio">Medio de pago.</param>
    /// <param name="operador">Operador que registra el pago.</param>
    /// <param name="referencia">Referencia única de la solicitud de pago.</param>
    /// <param name="fechaHora">Fecha y hora del pago, en UTC.</param>
    /// <returns>El pago registrado.</returns>
    /// <exception cref="InvalidOperationException">El cobro no está pendiente.</exception>
    /// <exception cref="ArgumentException">
    /// El operador está vacío o es muy largo, la referencia está vacía, o la fecha y la hora no están en UTC.
    /// </exception>
    public Pago Pagar(MedioDePago medio, string operador, Guid referencia, DateTimeOffset fechaHora)
    {
        if (Estado != EstadoDeCobro.Pendiente)
        {
            throw new InvalidOperationException("Solo se puede pagar un cobro pendiente.");
        }

        // Se crea el pago antes de cambiar el estado: si un dato es inválido, el cobro sigue pendiente.
        var pago = Pago.Registrar(Id, Valor, medio, operador, referencia, fechaHora);
        Estado = EstadoDeCobro.Pagado;

        return pago;
    }

    /// <summary>
    /// Indica si el cobro da pase vigente en un día operativo: está pagado y es de ese día (RN-03). La vigencia se calcula, sin guardar un estado Vencido.
    /// A las 06:00 empieza otro día operativo y el pase del anterior deja de valer solo.
    /// </summary>
    /// <param name="diaOperativo">Día operativo que se consulta.</param>
    /// <returns><see langword="true"/> si el cobro está pagado y es de ese día operativo.</returns>
    public bool DaPaseVigenteEn(DateOnly diaOperativo)
    {
        return Estado == EstadoDeCobro.Pagado && DiaOperativo == diaOperativo;
    }
}
