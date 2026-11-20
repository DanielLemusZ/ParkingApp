using Microsoft.AspNetCore.Mvc;

using ParkingApp.Aplicacion.Accesos;
using ParkingApp.Dominio.Accesos;

namespace ParkingApp.Web.Controllers;

/// <summary>
/// Controlador encargado de procesar las solicitudes de salida de vehículos.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public sealed class SalidaController : ControllerBase
{
    private readonly ProcesarSalidaUseCase _useCase;

    /// <summary>
    /// Inicializa una nueva instancia del controlador de salidas.
    /// </summary>
    /// <param name="useCase">Caso de uso para procesar la salida del vehículo.</param>
    public SalidaController(ProcesarSalidaUseCase useCase)
    {
        _useCase = useCase;
    }

    /// <summary>
    /// Intenta registrar la salida de un vehículo y determina si la barrera abre.
    /// </summary>
    /// <param name="request">Datos de lectura enviados por el carril de salida.</param>
    /// <param name="cancelacion">Token para cancelar la solicitud.</param>
    /// <returns>Resultado indicando si se autoriza la apertura de la barrera y el motivo.</returns>
    [HttpPost]
    [ProducesResponseType(typeof(ResultadoSalida), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ProcesarSalida([FromBody] SalidaRequest request, CancellationToken cancelacion)
    {
        try
        {
            var resultado = await _useCase.EjecutarAsync(
                request.Placa,
                request.Confianza,
                request.SedeId,
                request.Carril,
                cancelacion);

            return Ok(resultado);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}

/// <summary>
/// Datos que envía el simulador del carril de salida.
/// </summary>
public sealed class SalidaRequest
{
    /// <summary>
    /// Placa detectada del vehículo.
    /// </summary>
    public string Placa { get; set; } = string.Empty;

    /// <summary>
    /// Porcentaje de confianza de la lectura OCR (0 a 100).
    /// </summary>
    public double Confianza { get; set; } = 95;

    /// <summary>
    /// Identificador único de la sede (opcional; si se omite, se usa la sede del nodo).
    /// </summary>
    public Guid SedeId { get; set; }

    /// <summary>
    /// Número del carril de salida.
    /// </summary>
    public int Carril { get; set; } = 1;
}
