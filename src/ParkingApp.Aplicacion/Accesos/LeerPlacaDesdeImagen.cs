namespace ParkingApp.Aplicacion.Accesos;

/// <summary>
/// Caso de uso del carril: revisa la imagen que sube el operador y le pide al lector la placa y su confianza. La pantalla
/// usa el resultado en el mismo flujo del simulador. La imagen solo vive en memoria mientras se lee.
/// </summary>
public sealed class LeerPlacaDesdeImagen
{
    /// <summary>
    /// Tamaño máximo de la imagen: 5 MB.
    /// </summary>
    public const long TamanoMaximo = 5 * 1024 * 1024;

    /// <summary>
    /// Mensaje cuando el archivo no es JPG ni PNG.
    /// </summary>
    public const string SoloJpgOPng = "Solo se aceptan imágenes JPG o PNG";

    /// <summary>
    /// Mensaje cuando la imagen pasa de 5 MB.
    /// </summary>
    public const string ImagenMuyGrande = "La imagen no puede pasar de 5 MB";

    /// <summary>
    /// Mensaje cuando la imagen no se puede abrir.
    /// </summary>
    public const string ImagenDanada = "No se pudo abrir la imagen";

    /// <summary>
    /// Mensaje cuando la imagen no tiene una placa que se pueda leer.
    /// </summary>
    public const string SinPlaca = "No se encontró una placa en la imagen";

    /// <summary>
    /// Mensaje cuando el lector no está disponible, por ejemplo porque faltan los modelos.
    /// </summary>
    public const string NoSePudoLeer = "No se pudo leer la placa: escríbela a mano";

    /// <summary>
    /// Mensaje cuando la lectura tarda más de 5 segundos.
    /// </summary>
    public const string TiempoAgotado = "La lectura tardó más de 5 segundos";

    /// <summary>
    /// Tiempo máximo de una lectura.
    /// </summary>
    public static readonly TimeSpan TiempoMaximo = TimeSpan.FromSeconds(5);

    private readonly ILectorDePlacas lector;
    private readonly TimeProvider reloj;

    /// <summary>
    /// Crea el caso de uso con sus dependencias.
    /// </summary>
    /// <param name="lector">Lector de placas.</param>
    /// <param name="reloj">Reloj del sistema, para el tiempo máximo; en las pruebas, uno falso.</param>
    public LeerPlacaDesdeImagen(ILectorDePlacas lector, TimeProvider reloj)
    {
        this.lector = lector;
        this.reloj = reloj;
    }

    /// <summary>
    /// Texto que ve el operador para cada falla.
    /// </summary>
    /// <param name="falla">Por qué no se leyó la placa.</param>
    /// <returns>El mensaje.</returns>
    public static string Mensaje(FallaDeLectura falla)
    {
        return falla switch
        {
            FallaDeLectura.FormatoNoAdmitido => SoloJpgOPng,
            FallaDeLectura.ImagenMuyGrande => ImagenMuyGrande,
            FallaDeLectura.ImagenDanada => ImagenDanada,
            FallaDeLectura.SinPlaca => SinPlaca,
            FallaDeLectura.TiempoAgotado => TiempoAgotado,
            _ => NoSePudoLeer,
        };
    }

    /// <summary>
    /// Revisa la imagen y lee su placa.
    /// </summary>
    /// <param name="imagen">Contenido del archivo que subió el operador.</param>
    /// <param name="tamano">Tamaño del archivo, en bytes, para rechazarlo antes de leerlo si es muy grande.</param>
    /// <param name="cancelacion">Permite cancelar la operación.</param>
    /// <returns>La placa y su confianza, o por qué no se pudo leer.</returns>
    public async Task<LecturaDeImagen> EjecutarAsync(Stream imagen, long tamano, CancellationToken cancelacion = default)
    {
        ArgumentNullException.ThrowIfNull(imagen);

        // 1. Una imagen muy grande se rechaza sin leerla.
        if (tamano > TamanoMaximo)
        {
            return LecturaDeImagen.Fallida(FallaDeLectura.ImagenMuyGrande);
        }

        var contenido = await LeerHastaElMaximoAsync(imagen, cancelacion);

        if (contenido is null)
        {
            return LecturaDeImagen.Fallida(FallaDeLectura.ImagenMuyGrande);
        }

        // 2. El tipo se reconoce por el contenido y no por la extensión: un PDF renombrado como .jpg también se rechaza.
        if (!EsJpgOPng(contenido))
        {
            return LecturaDeImagen.Fallida(FallaDeLectura.FormatoNoAdmitido);
        }

        // 3. Si el lector tarda más del tiempo máximo, se corta y el operador escribe la placa a mano.
        try
        {
            return await lector.LeerAsync(contenido, cancelacion).WaitAsync(TiempoMaximo, reloj, cancelacion);
        }
        catch (TimeoutException)
        {
            return LecturaDeImagen.Fallida(FallaDeLectura.TiempoAgotado);
        }
    }

    // Lee el archivo completo en memoria, o devuelve null si pasa del tamaño máximo aunque haya dicho que no.
    private static async Task<byte[]?> LeerHastaElMaximoAsync(Stream imagen, CancellationToken cancelacion)
    {
        using var memoria = new MemoryStream();
        var bloque = new byte[81920];
        int leidos;

        while ((leidos = await imagen.ReadAsync(bloque, cancelacion)) > 0)
        {
            if (memoria.Length + leidos > TamanoMaximo)
            {
                return null;
            }

            memoria.Write(bloque, 0, leidos);
        }

        return memoria.ToArray();
    }

    // JPG empieza con FF D8 FF; PNG, con 89 50 4E 47 0D 0A 1A 0A.
    private static bool EsJpgOPng(byte[] contenido)
    {
        var esJpg = contenido.Length >= 3 && contenido[0] == 0xFF && contenido[1] == 0xD8 && contenido[2] == 0xFF;
        var esPng = contenido.Length >= 8 && contenido[0] == 0x89 && contenido[1] == 0x50 && contenido[2] == 0x4E
            && contenido[3] == 0x47 && contenido[4] == 0x0D && contenido[5] == 0x0A && contenido[6] == 0x1A && contenido[7] == 0x0A;

        return esJpg || esPng;
    }
}
