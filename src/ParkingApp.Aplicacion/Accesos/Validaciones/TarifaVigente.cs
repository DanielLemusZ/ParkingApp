using ParkingApp.Aplicacion.Cobros;
using ParkingApp.Aplicacion.Configuracion;
using ParkingApp.Aplicacion.Tarifas;
using ParkingApp.Dominio.Accesos;

namespace ParkingApp.Aplicacion.Accesos.Validaciones;

/// <summary>
/// Último eslabón: existe una tarifa vigente para el tipo de vehículo y la franja del ingreso.
/// Solo valida; el cobro lo genera <see cref="RegistrarIngreso"/> cuando la cadena deja pasar.
/// </summary>
public sealed class TarifaVigente : ValidacionDeIngreso
{
    private readonly IRepositorioTarifas tarifas;
    private readonly CalendarioOperativo calendario;

    /// <summary>
    /// Crea el eslabón con las tarifas y el calendario de la sede.
    /// </summary>
    /// <param name="tarifas">Donde se consultan las tarifas vigentes.</param>
    /// <param name="calendario">Calcula el día operativo y la franja del ingreso.</param>
    public TarifaVigente(IRepositorioTarifas tarifas, CalendarioOperativo calendario)
    {
        this.tarifas = tarifas;
        this.calendario = calendario;
    }

    /// <inheritdoc />
    protected override async Task<ResultadoDeValidacion> RevisarAsync(SolicitudDeIngreso solicitud, CancellationToken cancelacion)
    {
        ArgumentNullException.ThrowIfNull(solicitud);

        // Los eslabones anteriores ya garantizan que la placa tiene un formato reconocido.
        var tipoDeVehiculo = ClasificadorDeVehiculo.Clasificar(Placa.Crear(solicitud.Placa))!.Value;
        var momento = calendario.Ubicar(solicitud.FechaHora);
        var tarifa = await tarifas.ObtenerVigenteAsync(tipoDeVehiculo, momento.Franja, momento.DiaOperativo, cancelacion);

        return tarifa is null
            ? ResultadoDeValidacion.Rechazar(TextosDeCobro.SinTarifaVigente(tipoDeVehiculo, momento.Franja))
            : ResultadoDeValidacion.Aceptar();
    }
}
