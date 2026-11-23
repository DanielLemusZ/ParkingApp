using ParkingApp.Dominio.Accesos;

namespace ParkingApp.Aplicacion.Accesos;

/// <summary>
/// Lectura que llega al carril de entrada, o que el operador validó en la caja, y que recorre la cadena de validaciones.
/// </summary>
/// <param name="Placa">Texto de la placa, ya normalizado. Puede estar vacío.</param>
/// <param name="Confianza">Confianza de la lectura, que debería estar entre 0 y 100.</param>
/// <param name="Carril">Número del carril de entrada.</param>
/// <param name="FechaHora">Instante de la lectura o de su validación, en UTC. Todos los eslabones y el registro usan el mismo.</param>
/// <param name="Validacion">Lo que aportó el operador, o nada si la lectura es automática.</param>
public sealed record SolicitudDeIngreso(
    string Placa,
    int Confianza,
    int Carril,
    DateTimeOffset FechaHora,
    ValidacionDelOperador? Validacion = null)
{
    /// <summary>
    /// Tipo de vehículo del ingreso: el del formato de la placa y, si no se reconoce, el que indicó el operador.
    /// </summary>
    /// <returns>El tipo de vehículo, o <see langword="null"/> si todavía no se conoce.</returns>
    public TipoDeVehiculo? ObtenerTipoDeVehiculo()
    {
        // Aquí "Placa" es el texto de la solicitud, por eso la clase del dominio va con su espacio de nombres.
        var tipoPorFormato = ClasificadorDeVehiculo.Clasificar(Dominio.Accesos.Placa.Crear(Placa));

        if (tipoPorFormato != null)
        {
            return tipoPorFormato;
        }

        return Validacion?.TipoIndicado;
    }
}
