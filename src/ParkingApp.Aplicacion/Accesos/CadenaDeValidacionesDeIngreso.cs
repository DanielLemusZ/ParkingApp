namespace ParkingApp.Aplicacion.Accesos;

/// <summary>
/// Arma la cadena de validaciones del carril de entrada.
/// </summary>
public static class CadenaDeValidacionesDeIngreso
{
    /// <summary>
    /// Enlaza los eslabones en el orden recibido.
    /// </summary>
    /// <param name="eslabones">Eslabones en el orden en que se revisan. Deben ser instancias nuevas.</param>
    /// <returns>El primer eslabón de la cadena.</returns>
    /// <exception cref="ArgumentException">No se recibió ningún eslabón.</exception>
    public static ValidacionDeIngreso Crear(params ValidacionDeIngreso[] eslabones)
    {
        ArgumentNullException.ThrowIfNull(eslabones);

        if (eslabones.Length == 0)
        {
            throw new ArgumentException("La cadena necesita al menos un eslabón.", nameof(eslabones));
        }

        for (var posicion = 0; posicion < eslabones.Length - 1; posicion++)
        {
            eslabones[posicion].Enlazar(eslabones[posicion + 1]);
        }

        return eslabones[0];
    }
}
