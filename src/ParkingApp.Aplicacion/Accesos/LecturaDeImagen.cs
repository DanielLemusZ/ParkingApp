namespace ParkingApp.Aplicacion.Accesos;

/// <summary>
/// Resultado de leer la placa de una imagen: la placa y su confianza, o por qué no se pudo leer.
/// </summary>
/// <param name="Leida">Si se leyó una placa.</param>
/// <param name="Placa">Placa leída, o vacía si no se leyó.</param>
/// <param name="Confianza">Confianza de la lectura, de 0 a 100. Es 0 si no se leyó.</param>
/// <param name="Falla">Por qué no se leyó, o nada si se leyó.</param>
public sealed record LecturaDeImagen(bool Leida, string Placa, int Confianza, FallaDeLectura? Falla)
{
    /// <summary>
    /// Una lectura con placa.
    /// </summary>
    /// <param name="placa">Placa leída.</param>
    /// <param name="confianza">Confianza de la lectura, de 0 a 100.</param>
    /// <returns>La lectura.</returns>
    public static LecturaDeImagen Exitosa(string placa, int confianza)
    {
        return new LecturaDeImagen(true, placa, confianza, null);
    }

    /// <summary>
    /// Una lectura sin placa.
    /// </summary>
    /// <param name="falla">Por qué no se leyó.</param>
    /// <returns>La lectura.</returns>
    public static LecturaDeImagen Fallida(FallaDeLectura falla)
    {
        return new LecturaDeImagen(false, string.Empty, 0, falla);
    }
}
