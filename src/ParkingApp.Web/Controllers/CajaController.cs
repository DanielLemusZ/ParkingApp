using System.ComponentModel.DataAnnotations;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using ParkingApp.Aplicacion.Cobros;
using ParkingApp.Aplicacion.Identidad;
using ParkingApp.Aplicacion.Pagos;

namespace ParkingApp.Web.Controllers;

/// <summary>
/// API de la caja: lista los cobros pendientes y registra su pago en efectivo (F-04).
/// La usan el Operador y el Administrador (F-10): sin sesión responde 401.
/// </summary>
[Authorize(Roles = Roles.OperadorOAdministrador)]
[ApiController]
[Route("api/caja")]
public sealed class CajaController : ControllerBase
{
    private readonly ListarCobrosPendientes listarCobrosPendientes;
    private readonly RegistrarPagoEnEfectivo registrarPagoEnEfectivo;

    /// <summary>
    /// Crea el controlador con los casos de uso de la caja, los mismos que usa la pantalla.
    /// </summary>
    /// <param name="listarCobrosPendientes">Caso de uso que lista los cobros pendientes.</param>
    /// <param name="registrarPagoEnEfectivo">Caso de uso que registra el pago en efectivo.</param>
    public CajaController(ListarCobrosPendientes listarCobrosPendientes, RegistrarPagoEnEfectivo registrarPagoEnEfectivo)
    {
        this.listarCobrosPendientes = listarCobrosPendientes;
        this.registrarPagoEnEfectivo = registrarPagoEnEfectivo;
    }

    /// <summary>
    /// Lista los cobros pendientes de la sede, del más antiguo al más reciente.
    /// </summary>
    /// <param name="placa">Parte de la placa que se busca, por ejemplo <c>ABC</c>; vacío para listar todos.</param>
    /// <param name="cancelacion">Se cancela si el cliente cierra la conexión.</param>
    /// <returns>Los cobros pendientes, con la hora de la sede.</returns>
    [HttpGet("cobros-pendientes")]
    [ProducesResponseType<IReadOnlyList<CobroPendiente>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<CobroPendiente>>> ListarCobrosPendientesAsync(
        [FromQuery] string? placa,
        CancellationToken cancelacion)
    {
        var pendientes = await listarCobrosPendientes.EjecutarAsync(placa, cancelacion);
        return Ok(pendientes);
    }

    /// <summary>
    /// Registra el pago en efectivo de un cobro pendiente. Es idempotente: si la misma solicitud llega otra vez con la
    /// misma referencia, no se registra un segundo pago y se responde "El pago ya estaba registrado".
    /// </summary>
    /// <param name="solicitud">Cobro que se paga y referencia única de la solicitud.</param>
    /// <param name="cancelacion">Se cancela si el cliente cierra la conexión.</param>
    /// <returns>Si el pago se registró, el motivo y el pago.</returns>
    [HttpPost("pagos")]
    [ProducesResponseType<ResultadoDePago>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ResultadoDePago>> RegistrarPagoAsync(
        SolicitudDePagoHttp solicitud,
        CancellationToken cancelacion)
    {
        ArgumentNullException.ThrowIfNull(solicitud);

        // Si faltaba el cobro o la referencia, [Required] ya respondió 400 antes de llegar aquí.
        var cobroId = solicitud.CobroId.GetValueOrDefault();
        var referencia = solicitud.Referencia.GetValueOrDefault();

        if (referencia == Guid.Empty)
        {
            ModelState.AddModelError(nameof(solicitud.Referencia), "La referencia no puede estar vacía");
            return ValidationProblem(ModelState);
        }

        // El operador del pago es el usuario de la sesión (su correo). [Authorize] garantiza que hay uno.
        var operador = User.Identity?.Name ?? throw new InvalidOperationException("La solicitud no tiene un usuario con sesión.");

        var resultado = await registrarPagoEnEfectivo.EjecutarAsync(cobroId, referencia, operador, cancelacion);
        return resultado;
    }
}

/// <summary>
/// Cuerpo de la solicitud de pago en efectivo. Si falta un campo, la API responde 400 indicando cuál.
/// </summary>
public sealed class SolicitudDePagoHttp
{
    /// <summary>
    /// Identificador del cobro que se paga.
    /// </summary>
    [Required(ErrorMessage = "El cobro es obligatorio")]
    public Guid? CobroId { get; init; }

    /// <summary>
    /// Referencia única de la solicitud. El cliente la genera una vez por intento de pago y la repite en los reintentos.
    /// </summary>
    [Required(ErrorMessage = "La referencia es obligatoria")]
    public Guid? Referencia { get; init; }
}
