using ParkingApp.Dominio.Accesos;

namespace ParkingApp.Aplicacion.Accesos.Validaciones;

/// <summary>
/// Primer eslabón: la lectura trae una placa, una confianza de 0 a 100 y un carril válido.
/// </summary>
public sealed class LecturaValida : ValidacionDeIngreso
{
    /// <summary>
    /// Motivo cuando la placa llega vacía.
    /// </summary>
    public const string PlacaObligatoria = "La placa es obligatoria";

    /// <summary>
    /// Motivo cuando la confianza está fuera de rango.
    /// </summary>
    public const string ConfianzaFueraDeRango = "La confianza debe estar entre 0 y 100";

    /// <summary>
    /// Motivo cuando la lectura trae caracteres que no se pueden guardar, ni siquiera como lectura pendiente.
    /// </summary>
    public const string CaracteresNoValidos = "La placa tiene caracteres no válidos";

    /// <inheritdoc />
    protected override Task<ResultadoDeValidacion> RevisarAsync(SolicitudDeIngreso solicitud, CancellationToken cancelacion)
    {
        ArgumentNullException.ThrowIfNull(solicitud);

        ResultadoDeValidacion resultado;

        if (solicitud.Placa.Length == 0)
        {
            resultado = ResultadoDeValidacion.Rechazar(PlacaObligatoria);
        }
        else if (solicitud.Placa.Length > Placa.LargoMaximo)
        {
            resultado = ResultadoDeValidacion.Rechazar($"La placa no puede tener más de {Placa.LargoMaximo} caracteres");
        }
        else if (!Placa.SePuedeGuardar(solicitud.Placa))
        {
            resultado = ResultadoDeValidacion.Rechazar(CaracteresNoValidos);
        }
        else if (solicitud.Confianza < EventoDeAcceso.ConfianzaMinima || solicitud.Confianza > EventoDeAcceso.ConfianzaMaxima)
        {
            resultado = ResultadoDeValidacion.Rechazar(ConfianzaFueraDeRango);
        }
        else if (solicitud.Carril < 1)
        {
            resultado = ResultadoDeValidacion.Rechazar("El carril debe ser 1 o mayor");
        }
        else
        {
            resultado = ResultadoDeValidacion.Aceptar();
        }

        // Este eslabón no consulta nada externo, así que devuelve el resultado como una tarea ya terminada.
        return Task.FromResult(resultado);
    }
}
