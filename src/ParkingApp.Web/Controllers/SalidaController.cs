using Microsoft.AspNetCore.Mvc;

using ParkingApp.Aplicacion.Accesos;

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
    /// <returns>Resultado indicando si se autoriza la apertura de la barrera.</returns>
    [HttpPost]
    public async Task<IActionResult> ProcesarSalida([FromBody] SalidaRequest request)
    {
        try
        {
            var resultado = await _useCase.EjecutarAsync(
                request.Placa,
                request.Confianza,
                request.SedeId,
                request.Carril
            );

            return Ok(resultado);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}

/// <summary>
/// DTO con los datos que envía el simulador del carril de salida.
/// </summary>
public sealed class SalidaRequest
{
    /// <summary>
    /// Placa detectada del vehículo.
    /// </summary>
    public string Placa { get; set; } = string.Empty;

    /// <summary>
    /// Porcentaje de confianza de la lectura OCR.
    /// </summary>
    public double Confianza { get; set; }

    /// <summary>
    /// Identificador único de la sede.
    /// </summary>
    public Guid SedeId { get; set; }

    /// <summary>
    /// Número del carril de salida.
    /// </summary>
    public int Carril { get; set; }
}
