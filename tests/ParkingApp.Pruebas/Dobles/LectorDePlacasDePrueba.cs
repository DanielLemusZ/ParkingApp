using ParkingApp.Aplicacion.Accesos;

namespace ParkingApp.Pruebas.Dobles;

/// <summary>
/// Lector de placas de prueba: responde lo que cada prueba le indica, o nunca responde, y recuerda si lo llamaron.
/// </summary>
internal sealed class LectorDePlacasDePrueba : ILectorDePlacas
{
    public LecturaDeImagen Respuesta { get; set; } = LecturaDeImagen.Exitosa("ABC123", 97);

    /// <summary>
    /// Simula una lectura que se queda pensando, para probar el tiempo máximo.
    /// </summary>
    public bool NoResponde { get; set; }

    public int Llamadas { get; private set; }

    public Task<LecturaDeImagen> LeerAsync(byte[] imagen, CancellationToken cancelacion = default)
    {
        Llamadas++;

        if (NoResponde)
        {
            return new TaskCompletionSource<LecturaDeImagen>().Task;
        }

        return Task.FromResult(Respuesta);
    }
}
