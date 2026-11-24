using ParkingApp.Aplicacion.Configuracion;
using ParkingApp.Dominio.Accesos;
using ParkingApp.Dominio.Cobros;
using ParkingApp.Dominio.Configuracion;

namespace ParkingApp.Aplicacion.Cobros;

/// <summary>
/// Caso de uso de la caja: lista los cobros pendientes de la sede y permite buscarlos por placa.
/// </summary>
public sealed class ListarCobrosPendientes
{
    private readonly IRepositorioCobros cobros;
    private readonly ISedeActual sede;

    /// <summary>
    /// Crea el caso de uso con sus dependencias.
    /// </summary>
    /// <param name="cobros">Donde se consultan los cobros.</param>
    /// <param name="sede">Sede donde está la caja.</param>
    public ListarCobrosPendientes(IRepositorioCobros cobros, ISedeActual sede)
    {
        this.cobros = cobros;
        this.sede = sede;
    }

    /// <summary>
    /// Lista los cobros pendientes cuya placa contiene el texto buscado.
    /// </summary>
    /// <param name="placa">Parte de la placa, sin importar mayúsculas ni espacios; vacío para listar todos.</param>
    /// <param name="cancelacion">Permite cancelar la operación.</param>
    /// <returns>Los cobros pendientes, con la hora en la zona horaria de la sede.</returns>
    public async Task<IReadOnlyList<CobroPendiente>> EjecutarAsync(string? placa, CancellationToken cancelacion = default)
    {
        var filtro = Placa.Normalizar(placa);
        var pendientes = await cobros.ListarPendientesAsync(sede.Id, filtro, cancelacion);

        return pendientes
            .Select(cobro => new CobroPendiente(
                cobro.Id,
                cobro.Placa.Valor,
                TimeZoneInfo.ConvertTime(cobro.FechaHoraDeIngreso, sede.ZonaHoraria),
                cobro.Franja,
                cobro.Valor,
                cobro.DiaOperativo,
                cobro.Motivo))
            .ToList();
    }
}

/// <summary>
/// Cobro pendiente, como se muestra en la caja.
/// </summary>
/// <param name="Id">Identificador del cobro.</param>
/// <param name="Placa">Placa del vehículo.</param>
/// <param name="HoraDeIngreso">Hora en que entró el vehículo, en la hora de la sede; en un cobro por seguir adentro es la de días anteriores.</param>
/// <param name="Franja">Franja del cobro.</param>
/// <param name="Valor">Valor en pesos colombianos.</param>
/// <param name="DiaOperativo">Día operativo que cubre el cobro.</param>
/// <param name="Motivo">Por qué se generó: por el ingreso o porque el vehículo seguía adentro en un día nuevo.</param>
public sealed record CobroPendiente(
    Guid Id,
    string Placa,
    DateTimeOffset HoraDeIngreso,
    FranjaTarifaria Franja,
    decimal Valor,
    DateOnly DiaOperativo,
    MotivoDeCobro Motivo);
