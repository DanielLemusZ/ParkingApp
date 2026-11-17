using ParkingApp.Dominio.Accesos;

namespace ParkingApp.Aplicacion.Accesos.Validaciones;

/// <summary>
/// Rechaza las placas que no coinciden con un formato de carro o moto reconocido.
/// </summary>
public sealed class ClasificacionDeVehiculoValida : ValidacionDeIngreso
{
    /// <summary>
    /// Motivo mostrado cuando la placa no tiene un formato reconocido.
    /// </summary>
    public const string FormatoNoReconocido = "Formato no reconocido: ingreso manual en caja";

    /// <inheritdoc />
    protected override Task<ResultadoDeValidacion> RevisarAsync(SolicitudDeIngreso solicitud, CancellationToken cancelacion)
    {
        ArgumentNullException.ThrowIfNull(solicitud);
        var placa = Placa.Crear(solicitud.Placa);
        var resultado = ClasificadorDeVehiculo.Clasificar(placa) is null
            ? ResultadoDeValidacion.Rechazar(FormatoNoReconocido)
            : ResultadoDeValidacion.Aceptar();

        return Task.FromResult(resultado);
    }
}
