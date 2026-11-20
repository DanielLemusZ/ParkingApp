using Microsoft.Extensions.Logging;

using ParkingApp.Aplicacion.Pagos;

namespace ParkingApp.Infraestructura.ServiciosExternos;

/// <summary>
/// Implementación simulada de la facturación electrónica para entornos de desarrollo y MVP.
/// Registra las solicitudes en el log del sistema sin realizar llamadas HTTP externas.
/// </summary>
public sealed class AdaptadorFacturacionSimulado : IFacturacionElectronica
{
    private readonly ILogger<AdaptadorFacturacionSimulado> _logger;

    /// <summary>
    /// Inicializa una nueva instancia del adaptador simulado.
    /// </summary>
    /// <param name="logger">El servicio de registro de logs.</param>
    public AdaptadorFacturacionSimulado(ILogger<AdaptadorFacturacionSimulado> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc/>
    public Task SolicitarFacturaAsync(decimal valor, DateTimeOffset fecha, string concepto, string referencia, CancellationToken cancelacion = default)
    {
        if (_logger.IsEnabled(LogLevel.Information))
        {
            _logger.LogInformation(
                "Facturación electrónica solicitada con éxito. Referencia: {Referencia}, Valor: {Valor}, Fecha: {Fecha}, Concepto: {Concepto}",
                referencia, valor, fecha, concepto);
        }

        return Task.CompletedTask;
    }
}
