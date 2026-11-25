using Microsoft.Extensions.Logging;

using ParkingApp.Aplicacion.Pagos;

namespace ParkingApp.Infraestructura.ServiciosExternos.Pasarela;

/// <summary>
/// Envuelve la pasarela real con un respaldo (patrón Decorator, con la idea de un Circuit Breaker). Cuenta las fallas
/// técnicas seguidas (sin internet, demora, error del proveedor); un rechazo de la tarjeta no es una falla. Con
/// <see cref="OpcionesDePasarela.FallasAntesDeSoloEfectivo"/> fallas, la pasarela deja de estar disponible durante
/// <see cref="OpcionesDePasarela.MinutosSinPasarela"/> minutos y la caja cobra solo en efectivo; después se vuelve a
/// intentar. Si <see cref="OpcionesDePasarela.RespaldoSimulado"/> está activo (solo para la demo), en lugar de eso se
/// cobra con la pasarela simulada.
/// </summary>
public sealed partial class PasarelaConRespaldo : IPasarelaDePagos
{
    private readonly IPasarelaDePagos principal;
    private readonly AdaptadorPasarelaSimulada simulada;
    private readonly OpcionesDePasarela opciones;
    private readonly TimeProvider reloj;
    private readonly ILogger<PasarelaConRespaldo> registro;
    private readonly Lock candado = new Lock();

    private int fallasSeguidas;
    private DateTimeOffset sinPasarelaHasta = DateTimeOffset.MinValue;

    /// <summary>
    /// Crea la pasarela con respaldo.
    /// </summary>
    /// <param name="principal">Pasarela real, por ejemplo PayU.</param>
    /// <param name="simulada">Pasarela simulada, para el respaldo de la demo y sus transacciones.</param>
    /// <param name="opciones">Cuántas fallas se toleran, cuánto se espera y si hay respaldo simulado.</param>
    /// <param name="reloj">Reloj del sistema; en las pruebas, uno falso.</param>
    /// <param name="registro">Deja constancia de cada falla y de cuándo la caja queda solo en efectivo.</param>
    public PasarelaConRespaldo(
        IPasarelaDePagos principal,
        AdaptadorPasarelaSimulada simulada,
        OpcionesDePasarela opciones,
        TimeProvider reloj,
        ILogger<PasarelaConRespaldo> registro)
    {
        this.principal = principal;
        this.simulada = simulada;
        this.opciones = opciones;
        this.reloj = reloj;
        this.registro = registro;
    }

    /// <inheritdoc/>
    public bool Disponible => opciones.RespaldoSimulado || !SinPasarela();

    /// <inheritdoc/>
    public async Task<InicioDePagoEnPasarela> IniciarAsync(SolicitudDePagoEnPasarela solicitud, CancellationToken cancelacion = default)
    {
        if (!SinPasarela())
        {
            var inicio = await principal.IniciarAsync(solicitud, cancelacion);

            if (inicio.Disponible)
            {
                RegistrarExito();
                return inicio;
            }

            RegistrarFalla();
        }

        return opciones.RespaldoSimulado
            ? await simulada.IniciarAsync(solicitud, cancelacion)
            : new InicioDePagoEnPasarela(false, null);
    }

    /// <inheritdoc/>
    public async Task<ConsultaDePagoEnPasarela> ConsultarAsync(string transaccionId, CancellationToken cancelacion = default)
    {
        ArgumentNullException.ThrowIfNull(transaccionId);

        if (transaccionId.StartsWith(AdaptadorPasarelaSimulada.Prefijo, StringComparison.Ordinal))
        {
            return await simulada.ConsultarAsync(transaccionId, cancelacion);
        }

        var consulta = await principal.ConsultarAsync(transaccionId, cancelacion);

        if (consulta.Estado == EstadoEnPasarela.NoDisponible)
        {
            RegistrarFalla();
        }
        else
        {
            RegistrarExito();
        }

        return consulta;
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "La pasarela de pagos falló ({Fallas} seguidas).")]
    private static partial void FallaDeLaPasarela(ILogger registro, int fallas);

    [LoggerMessage(Level = LogLevel.Error, Message = "La pasarela de pagos no está disponible hasta {Hasta}: la caja cobra solo en efectivo.")]
    private static partial void CajaSoloEnEfectivo(ILogger registro, DateTimeOffset hasta);

    private bool SinPasarela()
    {
        lock (candado)
        {
            return reloj.GetUtcNow() < sinPasarelaHasta;
        }
    }

    private void RegistrarExito()
    {
        lock (candado)
        {
            fallasSeguidas = 0;
            sinPasarelaHasta = DateTimeOffset.MinValue;
        }
    }

    // Al pasar la espera se vuelve a intentar; si falla otra vez, se espera de nuevo sin contar desde cero.
    private void RegistrarFalla()
    {
        int fallas;
        DateTimeOffset? hasta = null;

        lock (candado)
        {
            fallas = ++fallasSeguidas;

            if (fallas >= opciones.FallasAntesDeSoloEfectivo)
            {
                sinPasarelaHasta = reloj.GetUtcNow().AddMinutes(opciones.MinutosSinPasarela);
                hasta = sinPasarelaHasta;
            }
        }

        FallaDeLaPasarela(registro, fallas);

        if (hasta is not null)
        {
            CajaSoloEnEfectivo(registro, hasta.Value);
        }
    }
}
