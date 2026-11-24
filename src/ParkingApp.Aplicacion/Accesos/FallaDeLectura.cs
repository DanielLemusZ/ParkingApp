namespace ParkingApp.Aplicacion.Accesos;

/// <summary>
/// Por qué no se pudo leer la placa de una imagen. En todos los casos el operador puede escribirla a mano.
/// </summary>
public enum FallaDeLectura
{
    /// <summary>
    /// El archivo no es una imagen JPG ni PNG.
    /// </summary>
    FormatoNoAdmitido,

    /// <summary>
    /// La imagen pasa del tamaño máximo.
    /// </summary>
    ImagenMuyGrande,

    /// <summary>
    /// El archivo dice ser JPG o PNG, pero no se puede abrir.
    /// </summary>
    ImagenDanada,

    /// <summary>
    /// La imagen se abrió, pero no tiene una placa que se pueda leer.
    /// </summary>
    SinPlaca,

    /// <summary>
    /// El lector no está disponible, por ejemplo porque faltan los archivos de los modelos.
    /// </summary>
    LectorNoDisponible,

    /// <summary>
    /// La lectura tardó más del tiempo máximo.
    /// </summary>
    TiempoAgotado,
}
