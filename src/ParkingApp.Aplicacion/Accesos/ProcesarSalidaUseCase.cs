using ParkingApp.Dominio.Accesos;

namespace ParkingApp.Aplicacion.Accesos;

/// <summary>
/// Caso de uso para procesar el intento de salida de un vehículo.
/// </summary>
public class ProcesarSalidaUseCase
{
    private readonly ISalidaRepository _repositorio;

    // Umbral de confianza configurado
    private const double UmbralConfianza = 80.0;

    /// <inheritdoc/>
    public ProcesarSalidaUseCase(ISalidaRepository repositorio)
    {
        _repositorio = repositorio;
    }

    /// <summary>
    /// Ejecuta la lógica para determinar si la barrera debe abrirse.
    /// </summary>
    // Nota: Actualizamos sede a Guid y carril a int
    public async Task<ResultadoSalida> EjecutarAsync(string placa, double confianza, Guid sedeId, int carril)
    {
        // 1. Criterio de error: Placa vacía
        if (string.IsNullOrWhiteSpace(placa))
        {
            throw new ArgumentException("La placa no puede estar vacía.");
        }

        // 2. Nota técnica F-05: Confianza menor al umbral (Pasa a validación)
        if (confianza < UmbralConfianza)
        {
            return new ResultadoSalida
            {
                AbreBarrera = false,
                Mensaje = "No abre: requiere validación de operador por baja confianza"
            };
        }

        // 3. Buscar cobros pendientes de cualquier día
        var valorPendiente = await _repositorio.ObtenerValorCobroPendienteAsync(placa);

        if (valorPendiente.HasValue)
        {
            return new ResultadoSalida
            {
                AbreBarrera = false,
                Mensaje = $"No abre: cobro pendiente de ${valorPendiente.Value:N0}" // Ej: $8.000
            };
        }

        // 4. Determinar si fue pagado o si salió sin cobro
        bool cobroPagado = await _repositorio.TieneCobroPagadoHoyAsync(placa);
        string mensajeExito = cobroPagado ? "Abre: cobro pagado" : "Abre: sin cobro";

        // 5. Criterio de éxito: Registrar la salida porque la barrera va a abrir
        // Usamos la variable correcta _repositorio
        await _repositorio.RegistrarSalidaAsync(
            placa,
            DateTimeOffset.UtcNow,
            sedeId,
            carril,
            (int)confianza
        );

        return new ResultadoSalida
        {
            AbreBarrera = true,
            Mensaje = mensajeExito
        };
    }
}
