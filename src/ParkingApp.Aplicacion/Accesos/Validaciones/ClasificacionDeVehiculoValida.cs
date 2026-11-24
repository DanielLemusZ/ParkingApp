using ParkingApp.Dominio.Accesos;

namespace ParkingApp.Aplicacion.Accesos.Validaciones;

/// <summary>
/// Envía a la caja las placas que no coinciden con un formato de carro o moto reconocido, salvo que el operador
/// ya haya indicado el tipo de vehículo al validarlas.
/// </summary>
public sealed class ClasificacionDeVehiculoValida : ValidacionDeIngreso
{
    /// <summary>
    /// Motivo mostrado cuando la placa no tiene un formato reconocido.
    /// </summary>
    public const string FormatoNoReconocido = "Formato no reconocido: hay que validar la lectura";

    /// <inheritdoc />
    protected override Task<ResultadoDeValidacion> RevisarAsync(SolicitudDeIngreso solicitud, CancellationToken cancelacion)
    {
        ArgumentNullException.ThrowIfNull(solicitud);
        var resultado = solicitud.ObtenerTipoDeVehiculo() is null
            ? ResultadoDeValidacion.EnviarACaja(FormatoNoReconocido, MotivoDeLecturaPendiente.FormatoNoReconocido)
            : ResultadoDeValidacion.Aceptar();

        return Task.FromResult(resultado);
    }
}
