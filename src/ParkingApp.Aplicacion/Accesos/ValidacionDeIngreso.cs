namespace ParkingApp.Aplicacion.Accesos;

/// <summary>
/// Eslabón de la cadena de validaciones del carril de entrada (patrón Chain of Responsibility).
/// Cada eslabón revisa una condición: si se cumple, pasa la lectura al siguiente; si no, detiene al vehículo con su motivo.
/// </summary>
/// <remarks>
/// Los eslabones guardan una referencia al siguiente, así que cada cadena debe armarse con instancias nuevas.
/// </remarks>
public abstract class ValidacionDeIngreso
{
    private ValidacionDeIngreso? siguiente;

    /// <summary>
    /// Enlaza el eslabón que se revisa después de este.
    /// </summary>
    /// <param name="eslabon">Eslabón siguiente.</param>
    /// <returns>El eslabón siguiente, para seguir encadenando.</returns>
    public ValidacionDeIngreso Enlazar(ValidacionDeIngreso eslabon)
    {
        ArgumentNullException.ThrowIfNull(eslabon);
        siguiente = eslabon;
        return eslabon;
    }

    /// <summary>
    /// Revisa este eslabón y, si deja pasar, los siguientes.
    /// </summary>
    /// <param name="solicitud">Lectura que llega al carril.</param>
    /// <param name="cancelacion">Permite cancelar la operación.</param>
    /// <returns>El resultado del primer eslabón que detiene al vehículo, o un resultado que permite el paso.</returns>
    public async Task<ResultadoDeValidacion> ValidarAsync(SolicitudDeIngreso solicitud, CancellationToken cancelacion = default)
    {
        var resultado = await RevisarAsync(solicitud, cancelacion);

        if (!resultado.Permite || siguiente is null)
        {
            return resultado;
        }

        return await siguiente.ValidarAsync(solicitud, cancelacion);
    }

    /// <summary>
    /// Revisa la condición propia de este eslabón.
    /// </summary>
    /// <param name="solicitud">Lectura que llega al carril.</param>
    /// <param name="cancelacion">Permite cancelar la operación.</param>
    /// <returns>Si el vehículo puede seguir o por qué se detiene.</returns>
    protected abstract Task<ResultadoDeValidacion> RevisarAsync(SolicitudDeIngreso solicitud, CancellationToken cancelacion);
}
