using Microsoft.AspNetCore.Mvc;
using ParkingApp.Aplicacion.Accesos;

namespace ParkingApp.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SalidaController : ControllerBase
{
    private readonly ProcesarSalidaUseCase _useCase;

    public SalidaController(ProcesarSalidaUseCase useCase)
    {
        _useCase = useCase;
    }

    /// <summary>
    /// Intenta registrar la salida de un vehículo y determina si la barrera abre.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> ProcesarSalida([FromBody] SalidaRequest request)
    {
        try
        {
            var resultado = await _useCase.EjecutarAsync(
                request.Placa, 
                request.Confianza, 
                request.Sede, 
                request.Carril
            );
            
            return Ok(resultado);
        }
        catch (ArgumentException ex)
        {
            // Atrapa la placa vacía y responde con el error de validación exigido
            return BadRequest(new { error = ex.Message });
        }
    }
}

/// <summary>
/// DTO con los datos que envía el simulador del carril de salida.
/// </summary>
public class SalidaRequest
{
    public string Placa { get; set; } = string.Empty;
    public double Confianza { get; set; }
    public string Sede { get; set; } = string.Empty;
    public string Carril { get; set; } = string.Empty;
}

public class SalidaRequest
{
    public string Placa { get; set; } = string.Empty;
    public double Confianza { get; set; } // O int, dependiendo de cómo te lo mande el simulador
    public Guid SedeId { get; set; } // Cambió a Guid
    public int Carril { get; set; }  // Cambió a int
}