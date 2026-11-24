namespace ParkingApp.Infraestructura.LecturaDePlacas;

/// <summary>
/// Convierte la salida del OCR de placas en el texto de la placa y su confianza.
/// </summary>
public static class TextoDelOcr
{
    /// <summary>
    /// Caracteres que el OCR puede devolver en cada posición, en el orden de su salida.
    /// </summary>
    public const string Alfabeto = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ_";

    /// <summary>
    /// Carácter de relleno de las posiciones vacías: no forma parte de la placa.
    /// </summary>
    public const char Relleno = '_';

    /// <summary>
    /// Toma el carácter más probable de cada posición y descarta el relleno. La confianza es la menor de las
    /// probabilidades de los caracteres leídos, en la escala de 0 a 100: basta un carácter dudoso para que la
    /// lectura entera sea dudosa.
    /// </summary>
    /// <param name="probabilidades">Probabilidades de cada carácter del alfabeto, posición por posición.</param>
    /// <returns>La placa y su confianza; la placa vacía y confianza 0 si solo hay relleno.</returns>
    public static (string Placa, int Confianza) Decodificar(float[] probabilidades)
    {
        ArgumentNullException.ThrowIfNull(probabilidades);

        var caracteres = Alfabeto.Length;
        var posiciones = probabilidades.Length / caracteres;
        var placa = string.Empty;
        var menor = 1f;

        for (var posicion = 0; posicion < posiciones; posicion++)
        {
            var inicio = posicion * caracteres;
            var mejor = 0;

            for (var indice = 1; indice < caracteres; indice++)
            {
                if (probabilidades[inicio + indice] > probabilidades[inicio + mejor])
                {
                    mejor = indice;
                }
            }

            if (Alfabeto[mejor] != Relleno)
            {
                placa += Alfabeto[mejor];
                menor = Math.Min(menor, probabilidades[inicio + mejor]);
            }
        }

        if (placa.Length == 0)
        {
            return (string.Empty, 0);
        }

        var confianza = (int)Math.Floor(menor * 100);
        return (placa, Math.Clamp(confianza, 0, 100));
    }
}
