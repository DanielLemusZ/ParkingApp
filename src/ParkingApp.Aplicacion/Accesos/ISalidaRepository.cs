using ParkingApp.Dominio.Accesos;

namespace ParkingApp.Aplicacion.Accesos;

/// <summary>
/// Contrato para interactuar con la base de datos durante el proceso de salida de vehículos.
/// </summary>
public interface ISalidaRepository
{
    /// <summary>
    /// Busca si existe algún cobro pendiente asociado a la placa, sin importar el día operativo.
    /// </summary>
    /// <param name="placa">Placa normalizada del vehículo.</param>
    /// <param name="cancelacion">Token para cancelar la operación.</param>
    /// <returns>El valor del cobro pendiente, o <see langword="null"/> si no tiene deudas.</returns>
    Task<decimal?> ObtenerValorCobroPendienteAsync(Placa placa, CancellationToken cancelacion = default);

    /// <summary>
    /// Verifica si el vehículo tiene un cobro pagado en el día operativo indicado.
    /// </summary>
    /// <param name="placa">Placa normalizada del vehículo.</param>
    /// <param name="diaOperativo">Día operativo de la sede.</param>
    /// <param name="cancelacion">Token para cancelar la operación.</param>
    /// <returns><see langword="true"/> si tiene un cobro pagado en ese día; de lo contrario, <see langword="false"/>.</returns>
    Task<bool> TieneCobroPagadoEnDiaOperativoAsync(Placa placa, DateOnly diaOperativo, CancellationToken cancelacion = default);

    /// <summary>
    /// Obtiene el umbral mínimo de confianza configurado para la sede.
    /// </summary>
    /// <param name="sedeId">Identificador de la sede.</param>
    /// <param name="cancelacion">Token para cancelar la operación.</param>
    /// <returns>El umbral de confianza de 0 a 100.</returns>
    Task<int> ObtenerUmbralConfianzaAsync(Guid sedeId, CancellationToken cancelacion = default);

    /// <summary>
    /// Registra la salida exitosa del vehículo en el sistema.
    /// </summary>
    /// <param name="placa">Placa normalizada del vehículo que sale.</param>
    /// <param name="tipoDeVehiculo">Tipo de vehículo clasificado.</param>
    /// <param name="fechaHoraUtc">Fecha y hora exacta de la salida en UTC.</param>
    /// <param name="sedeId">Identificador de la sede.</param>
    /// <param name="carril">Número del carril de salida.</param>
    /// <param name="confianza">Porcentaje de confianza de la lectura.</param>
    /// <param name="cancelacion">Token para cancelar la operación.</param>
    /// <returns>Tarea que representa la operación asíncrona.</returns>
    Task RegistrarSalidaAsync(
        Placa placa,
        TipoDeVehiculo tipoDeVehiculo,
        DateTimeOffset fechaHoraUtc,
        Guid sedeId,
        int carril,
        int confianza,
        CancellationToken cancelacion = default);
}
