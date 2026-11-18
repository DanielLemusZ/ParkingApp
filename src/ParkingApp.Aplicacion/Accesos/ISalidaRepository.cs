namespace ParkingApp.Aplicacion.Accesos;

/// <summary>
/// Contrato para interactuar con la base de datos durante el proceso de salida de vehículos.
/// </summary>
public interface ISalidaRepository
{
    /// <summary>
    /// Busca si existe algún cobro pendiente asociado a la placa, sin importar el día operativo.
    /// </summary>
    /// <param name="placa">Placa del vehículo.</param>
    /// <returns>El valor del cobro pendiente, o null si no tiene deudas.</returns>
    Task<decimal?> ObtenerValorCobroPendienteAsync(string placa);

    /// <summary>
    /// Verifica si el vehículo tiene un cobro pagado en el día operativo actual.
    /// </summary>
    /// <param name="placa">Placa del vehículo.</param>
    /// <returns>Verdadero si tiene un cobro pagado hoy, falso en caso contrario.</returns>
    Task<bool> TieneCobroPagadoHoyAsync(string placa);

    /// <summary>
    /// Registra la salida exitosa del vehículo en el sistema.
    /// </summary>
    /// <param name="placa">Placa del vehículo que sale.</param>
    /// <param name="fechaHora">Fecha y hora exacta de la salida.</param>
    /// <param name="sede">Nombre de la sede.</param>
    /// <param name="carril">Nombre o identificador del carril.</param>
    /// <returns>Tarea que representa la operación asíncrona.</returns>
    Task RegistrarSalidaAsync(string placa, DateTimeOffset fechaHora, Guid sedeId, int carril, int confianza);
}