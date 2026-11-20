using System.ComponentModel.DataAnnotations;

using Microsoft.AspNetCore.Mvc;

using ParkingApp.Aplicacion.Accesos;

namespace ParkingApp.Web.Controllers;

/// <summary>
/// API del carril: recibe la placa y la confianza, como las entregaría una cámara LPR.
/// </summary>
[ApiController]
[Route("api/carril")]
public sealed class CarrilController : ControllerBase
{
    private readonly RegistrarIngreso registrarIngreso;
    private readonly RegistrarSalida registrarSalida;

    /// <summary>
    /// Crea el controlador con los casos de uso del carril de entrada y del de salida, los mismos que usa la pantalla.
    /// </summary>
    /// <param name="registrarIngreso">Caso de uso del carril de entrada.</param>
    /// <param name="registrarSalida">Caso de uso del carril de salida.</param>
    public CarrilController(RegistrarIngreso registrarIngreso, RegistrarSalida registrarSalida)
    {
        this.registrarIngreso = registrarIngreso;
        this.registrarSalida = registrarSalida;
    }

    /// <summary>
    /// Registra el ingreso de un vehículo por un carril de entrada.
    /// </summary>
    /// <param name="solicitud">Placa leída, confianza de la lectura y carril.</param>
    /// <param name="cancelacion">Se cancela si el cliente cierra la conexión.</param>
    /// <returns>Si la barrera abre, el motivo y el ingreso registrado.</returns>
    [HttpPost("ingresos")]
    [ProducesResponseType<ResultadoDeIngreso>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ResultadoDeIngreso>> RegistrarIngresoAsync(
        SolicitudDeIngresoHttp solicitud,
        CancellationToken cancelacion)
    {
        ArgumentNullException.ThrowIfNull(solicitud);

        // Si faltaba la confianza o el carril, [Required] ya respondió 400 antes de llegar aquí.
        var confianza = solicitud.Confianza.GetValueOrDefault();
        var carril = solicitud.Carril.GetValueOrDefault();

        var resultado = await registrarIngreso.EjecutarAsync(solicitud.Placa, confianza, carril, cancelacion);
        return resultado;
    }

    /// <summary>
    /// Registra la salida de un vehículo por un carril de salida. La barrera abre solo si no tiene cobros pendientes.
    /// </summary>
    /// <param name="solicitud">Placa leída, confianza de la lectura y carril.</param>
    /// <param name="cancelacion">Se cancela si el cliente cierra la conexión.</param>
    /// <returns>Si la barrera abre, el motivo y la salida registrada.</returns>
    [HttpPost("salidas")]
    [ProducesResponseType<ResultadoDeSalida>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ResultadoDeSalida>> RegistrarSalidaAsync(
        SolicitudDeSalidaHttp solicitud,
        CancellationToken cancelacion)
    {
        ArgumentNullException.ThrowIfNull(solicitud);

        // Si faltaba la confianza o el carril, [Required] ya respondió 400 antes de llegar aquí.
        var confianza = solicitud.Confianza.GetValueOrDefault();
        var carril = solicitud.Carril.GetValueOrDefault();

        var resultado = await registrarSalida.EjecutarAsync(solicitud.Placa, confianza, carril, cancelacion);
        return resultado;
    }
}

/// <summary>
/// Cuerpo de la solicitud de ingreso. Si falta un campo, la API responde 400 indicando cuál.
/// </summary>
public sealed class SolicitudDeIngresoHttp
{
    /// <summary>
    /// Placa leída, por ejemplo <c>ABC123</c> (carro) o <c>XYZ12D</c> (moto).
    /// </summary>
    [Required(ErrorMessage = "La placa es obligatoria")]
    public string? Placa { get; init; }

    /// <summary>
    /// Confianza de la lectura, de 0 a 100.
    /// </summary>
    [Required(ErrorMessage = "La confianza es obligatoria")]
    [Range(0, 100, ErrorMessage = "La confianza debe estar entre 0 y 100")]
    public int? Confianza { get; init; }

    /// <summary>
    /// Número del carril de entrada, desde 1.
    /// </summary>
    [Required(ErrorMessage = "El carril es obligatorio")]
    [Range(1, 99, ErrorMessage = "El carril debe ser 1 o mayor")]
    public int? Carril { get; init; }
}

/// <summary>
/// Cuerpo de la solicitud de salida. Si falta un campo, la API responde 400 indicando cuál.
/// </summary>
public sealed class SolicitudDeSalidaHttp
{
    /// <summary>
    /// Placa leída. Puede no tener un formato reconocido, si el vehículo entró por ingreso manual.
    /// </summary>
    [Required(ErrorMessage = "La placa es obligatoria")]
    public string? Placa { get; init; }

    /// <summary>
    /// Confianza de la lectura, de 0 a 100.
    /// </summary>
    [Required(ErrorMessage = "La confianza es obligatoria")]
    [Range(0, 100, ErrorMessage = "La confianza debe estar entre 0 y 100")]
    public int? Confianza { get; init; }

    /// <summary>
    /// Número del carril de salida, desde 1.
    /// </summary>
    [Required(ErrorMessage = "El carril es obligatorio")]
    [Range(1, 99, ErrorMessage = "El carril debe ser 1 o mayor")]
    public int? Carril { get; init; }
}
