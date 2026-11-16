namespace ParkingApp.Aplicacion.Accesos;

/// <summary>
/// Respuesta de un eslabón de la cadena de validaciones: deja pasar al vehículo o lo detiene con un motivo.
/// </summary>
/// <param name="Permite">Si el vehículo puede seguir.</param>
/// <param name="Motivo">Por qué se detiene. Vacío si el vehículo puede seguir.</param>
public sealed record ResultadoDeValidacion(bool Permite, string Motivo)
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
}
