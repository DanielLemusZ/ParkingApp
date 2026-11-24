namespace ParkingApp.Aplicacion.Accesos;

/// <summary>
/// Lee la placa de una imagen, como lo haría la cámara del carril. La implementación del MVP es un OCR que corre en el
/// mismo equipo; se puede cambiar por otra sin tocar el flujo del carril.
/// </summary>
public interface ILectorDePlacas
{
    /// <summary>
    /// Lee la placa de una imagen JPG o PNG que ya se revisó. Una imagen dañada, sin placa o un lector sin modelos se
    /// responden con su falla, no con una excepción. La imagen no se guarda.
    /// </summary>
    /// <param name="imagen">Contenido del archivo.</param>
    /// <param name="cancelacion">Permite cancelar la operación.</param>
    /// <returns>La placa y su confianza, o por qué no se pudo leer.</returns>
    Task<LecturaDeImagen> LeerAsync(byte[] imagen, CancellationToken cancelacion = default);
}
