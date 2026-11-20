using ParkingApp.Aplicacion.Cobros;
using ParkingApp.Aplicacion.Configuracion;
using ParkingApp.Dominio.Accesos;

namespace ParkingApp.Aplicacion.Accesos;

/// <summary>
/// Caso de uso para procesar el intento de salida de un vehículo.
/// </summary>
public sealed class ProcesarSalidaUseCase
{
    private readonly ISalidaRepository _repositorio;
    private readonly ISedeActual _sedeActual;
    private readonly CalendarioOperativo _calendario;
    private readonly TimeProvider _reloj;

    /// <summary>
    /// Inicializa una nueva instancia del caso de uso de salida.
    /// </summary>
    /// <param name="repositorio">Repositorio de salidas y consulta de cobros.</param>
    /// <param name="sedeActual">Sede actual donde opera el carril.</param>
    /// <param name="calendario">Calendario operativo para determinar el día operativo.</param>
    /// <param name="reloj">Proveedor de fecha y hora del sistema.</param>
    public ProcesarSalidaUseCase(
        ISalidaRepository repositorio,
        ISedeActual sedeActual,
        CalendarioOperativo calendario,
        TimeProvider reloj)
    {
        _repositorio = repositorio;
        _sedeActual = sedeActual;
        _calendario = calendario;
        _reloj = reloj;
    }

    /// <summary>
    /// Ejecuta la lógica para determinar si la barrera debe abrirse y registra la salida si corresponde.
    /// </summary>
    /// <param name="placa">Texto leído de la placa.</param>
    /// <param name="confianza">Porcentaje de confianza de la lectura.</param>
    /// <param name="sedeId">Identificador de la sede (si es <see cref="Guid.Empty"/>, usa la sede actual).</param>
    /// <param name="carril">Número del carril de salida.</param>
    /// <param name="cancelacion">Token para cancelar la operación.</param>
    /// <returns>Resultado indicando si la barrera abre, el motivo y los datos de la salida registrada.</returns>
    public async Task<ResultadoSalida> EjecutarAsync(
        string? placa,
        double confianza,
        Guid sedeId,
        int carril,
        CancellationToken cancelacion = default)
    {
        // 1. Criterio de error: Placa vacía o solo espacios/guiones
        if (string.IsNullOrWhiteSpace(Placa.Normalizar(placa)))
        {
            throw new ArgumentException("La placa no puede estar vacía.", nameof(placa));
        }

        var placaNormalizada = Placa.Crear(placa);
        var sedeEfectiva = sedeId == Guid.Empty ? _sedeActual.Id : sedeId;

        // 2. Nota técnica F-05: Confianza menor al umbral de la sede (pasa a validación en caja)
        var umbralConfianza = await _repositorio.ObtenerUmbralConfianzaAsync(sedeEfectiva, cancelacion);

        if (confianza < umbralConfianza)
        {
            return new ResultadoSalida
            {
                AbreBarrera = false,
                Mensaje = "No abre: requiere validación de operador por baja confianza"
            };
        }

        // 3. Criterio 3: Buscar cobros pendientes de cualquier día operativo
        var valorPendiente = await _repositorio.ObtenerValorCobroPendienteAsync(placaNormalizada, cancelacion);

        if (valorPendiente.HasValue)
        {
            return new ResultadoSalida
            {
                AbreBarrera = false,
                Mensaje = $"No abre: cobro pendiente de {TextosDeCobro.Pesos(valorPendiente.Value)}"
            };
        }

        // 4. Criterio 2 y 4: Determinar si tiene cobro pagado en el día operativo actual o sale sin cobro
        var ahoraEnUtc = _reloj.GetUtcNow();
        var momento = await _calendario.UbicarAsync(ahoraEnUtc, cancelacion);
        var cobroPagado = await _repositorio.TieneCobroPagadoEnDiaOperativoAsync(placaNormalizada, momento.DiaOperativo, cancelacion);
        var mensajeExito = cobroPagado ? "Abre: cobro pagado" : "Abre: sin cobro";

        // 5. Criterio 1 y 7: Clasificar (permitiendo placas manuales como NoClasificado) y registrar la salida
        var tipoDeVehiculo = ClasificadorDeVehiculo.Clasificar(placaNormalizada) ?? TipoDeVehiculo.NoClasificado;
        var confianzaEntera = (int)Math.Round(confianza, MidpointRounding.AwayFromZero);

        await _repositorio.RegistrarSalidaAsync(
            placaNormalizada,
            tipoDeVehiculo,
            ahoraEnUtc,
            sedeEfectiva,
            carril,
            confianzaEntera,
            cancelacion);

        return new ResultadoSalida
        {
            AbreBarrera = true,
            Mensaje = mensajeExito,
            Placa = placaNormalizada.Valor,
            TipoDeVehiculo = tipoDeVehiculo,
            FechaHora = momento.HoraLocal,
            SedeId = sedeEfectiva,
            Carril = carril,
            Confianza = confianzaEntera
        };
    }
}
