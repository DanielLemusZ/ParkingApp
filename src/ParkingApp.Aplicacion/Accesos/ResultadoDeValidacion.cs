using ParkingApp.Dominio.Accesos;

namespace ParkingApp.Aplicacion.Accesos;

/// <summary>
/// Respuesta de un eslabón de la cadena de validaciones: deja pasar al vehículo o lo detiene con un motivo.
/// </summary>
/// <param name="Permite">Si el vehículo puede seguir.</param>
/// <param name="Motivo">Por qué se detiene. Vacío si el vehículo puede seguir.</param>
/// <param name="MotivoPendiente">Si tiene valor, la lectura pasa a la caja para que el operador la valide.</param>
public sealed record ResultadoDeValidacion(bool Permite, string Motivo, MotivoDeLecturaPendiente? MotivoPendiente = null)
{
    /// <summary>
    /// El vehículo puede seguir al siguiente eslabón.
    /// </summary>
    /// <returns>Un resultado que permite el paso.</returns>
    public static ResultadoDeValidacion Aceptar()
    {
        return new ResultadoDeValidacion(true, string.Empty);
    }

    /// <summary>
    /// El vehículo se detiene aquí.
    /// </summary>
    /// <param name="motivo">Motivo que se muestra en el carril.</param>
    /// <returns>Un resultado que detiene el paso.</returns>
    public static ResultadoDeValidacion Rechazar(string motivo)
    {
        return new ResultadoDeValidacion(false, motivo);
    }

    /// <summary>
    /// El vehículo se detiene aquí y su lectura queda pendiente de validación en la caja.
    /// </summary>
    /// <param name="motivo">Motivo que se muestra en el carril.</param>
    /// <param name="motivoPendiente">Por qué la debe revisar el operador.</param>
    /// <returns>Un resultado que detiene el paso y envía la lectura a la caja.</returns>
    public static ResultadoDeValidacion EnviarACaja(string motivo, MotivoDeLecturaPendiente motivoPendiente)
    {
        return new ResultadoDeValidacion(false, motivo, motivoPendiente);
    }
}
