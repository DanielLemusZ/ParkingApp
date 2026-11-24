using System.ComponentModel.DataAnnotations;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using ParkingApp.Aplicacion.Accesos;
using ParkingApp.Aplicacion.Cobros;
using ParkingApp.Aplicacion.Identidad;
using ParkingApp.Aplicacion.Pagos;
using ParkingApp.Dominio.Accesos;

namespace ParkingApp.Web.Controllers;

/// <summary>
/// API de la caja: lista los cobros pendientes y registra su pago en efectivo (F-04), y lista y valida las lecturas
/// de placa dudosas (F-06).
/// La usan el Operador y el Administrador (F-10): sin sesión responde 401.
/// </summary>
[Authorize(Roles = Roles.OperadorOAdministrador)]
[ApiController]
[Route("api/caja")]
public sealed class CajaController : ControllerBase
{
    private readonly ListarCobrosPendientes listarCobrosPendientes;
    private readonly RegistrarPagoEnEfectivo registrarPagoEnEfectivo;
    private readonly RegistrarPagoDeLaPlacaEnEfectivo registrarPagoDeLaPlacaEnEfectivo;
    private readonly ListarLecturasPendientes listarLecturasPendientes;
    private readonly ValidarLectura validarLectura;

    /// <summary>
    /// Crea el controlador con los casos de uso de la caja, los mismos que usa la pantalla.
    /// </summary>
    /// <param name="listarCobrosPendientes">Caso de uso que lista los cobros pendientes.</param>
    /// <param name="registrarPagoEnEfectivo">Caso de uso que registra el pago en efectivo.</param>
    /// <param name="registrarPagoDeLaPlacaEnEfectivo">Caso de uso que paga todo lo pendiente de una placa.</param>
    /// <param name="listarLecturasPendientes">Caso de uso que lista las placas por revisar.</param>
    /// <param name="validarLectura">Caso de uso que valida una lectura.</param>
    public CajaController(
        ListarCobrosPendientes listarCobrosPendientes,
        RegistrarPagoEnEfectivo registrarPagoEnEfectivo,
        RegistrarPagoDeLaPlacaEnEfectivo registrarPagoDeLaPlacaEnEfectivo,
        ListarLecturasPendientes listarLecturasPendientes,
        ValidarLectura validarLectura)
    {
        this.listarCobrosPendientes = listarCobrosPendientes;
        this.registrarPagoEnEfectivo = registrarPagoEnEfectivo;
        this.registrarPagoDeLaPlacaEnEfectivo = registrarPagoDeLaPlacaEnEfectivo;
        this.listarLecturasPendientes = listarLecturasPendientes;
        this.validarLectura = validarLectura;
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

        // El operador del pago es el usuario de la sesión (su correo).
        var operador = OperadorDeLaSesion();

        var resultado = await registrarPagoEnEfectivo.EjecutarAsync(cobroId, referencia, operador, cancelacion);
        return resultado;
    }

    /// <summary>
    /// Paga en efectivo, de una vez, todos los cobros pendientes de una placa, de cualquier día operativo (#17). Es
    /// idempotente: si la misma solicitud llega otra vez con la misma referencia, responde "El pago ya estaba registrado".
    /// </summary>
    /// <param name="solicitud">Placa que paga y referencia única de la solicitud.</param>
    /// <param name="cancelacion">Se cancela si el cliente cierra la conexión.</param>
    /// <returns>Si el pago se registró, el motivo y el pago con el total.</returns>
    [HttpPost("pagos/placa")]
    [ProducesResponseType<ResultadoDePagoDeLaPlaca>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ResultadoDePagoDeLaPlaca>> RegistrarPagoDeLaPlacaAsync(
        SolicitudDePagoDeLaPlacaHttp solicitud,
        CancellationToken cancelacion)
    {
        ArgumentNullException.ThrowIfNull(solicitud);

        // Si faltaba la placa o la referencia, [Required] ya respondió 400 antes de llegar aquí.
        var referencia = solicitud.Referencia.GetValueOrDefault();

        if (referencia == Guid.Empty)
        {
            ModelState.AddModelError(nameof(solicitud.Referencia), "La referencia no puede estar vacía");
            return ValidationProblem(ModelState);
        }

        var operador = OperadorDeLaSesion();

        var resultado = await registrarPagoDeLaPlacaEnEfectivo.EjecutarAsync(solicitud.Placa, referencia, operador, cancelacion);
        return resultado;
    }

    /// <summary>
    /// Lista las lecturas que esperan validación, de la más antigua a la más reciente.
    /// </summary>
    /// <param name="cancelacion">Se cancela si el cliente cierra la conexión.</param>
    /// <returns>Las lecturas pendientes, con la hora de la sede.</returns>
    [HttpGet("lecturas-pendientes")]
    [ProducesResponseType<IReadOnlyList<LecturaPorRevisar>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<LecturaPorRevisar>>> ListarLecturasPendientesAsync(CancellationToken cancelacion)
    {
        var pendientes = await listarLecturasPendientes.EjecutarAsync(cancelacion);
        return Ok(pendientes);
    }

    /// <summary>
    /// Confirma o corrige la placa de una lectura pendiente. El vehículo sigue el flujo de su carril y el evento queda
    /// como lectura manual. La lectura queda validada aunque la barrera no abra; una segunda validación se rechaza.
    /// </summary>
    /// <param name="id">Lectura que se valida.</param>
    /// <param name="solicitud">Placa validada y, si su formato no se reconoce en la entrada, el tipo de vehículo.</param>
    /// <param name="cancelacion">Se cancela si el cliente cierra la conexión.</param>
    /// <returns>Si la lectura se validó, si la barrera abre y el motivo.</returns>
    [HttpPost("lecturas-pendientes/{id:guid}/validacion")]
    [ProducesResponseType<ResultadoDeValidarLectura>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ResultadoDeValidarLectura>> ValidarLecturaAsync(
        Guid id,
        SolicitudDeValidacionHttp solicitud,
        CancellationToken cancelacion)
    {
        ArgumentNullException.ThrowIfNull(solicitud);

        var operador = OperadorDeLaSesion();

        var resultado = await validarLectura.EjecutarAsync(id, solicitud.Placa, solicitud.TipoDeVehiculo, operador, cancelacion);
        return resultado;
    }

    // [Authorize] garantiza que hay un usuario.
    private string OperadorDeLaSesion()
    {
        return User.Identity?.Name ?? throw new InvalidOperationException("La solicitud no tiene un usuario con sesión.");
    }
}

/// <summary>
/// Cuerpo de la solicitud que valida una lectura. Si falta la placa, la API responde 400.
/// </summary>
public sealed class SolicitudDeValidacionHttp
{
    /// <summary>
    /// Placa confirmada o digitada. Solo admite letras y números; los espacios y los guiones se quitan.
    /// </summary>
    [Required(ErrorMessage = "La placa es obligatoria")]
    public string? Placa { get; init; }

    /// <summary>
    /// 0 = carro, 1 = moto. Solo hace falta en la entrada, si el formato de la placa no se reconoce.
    /// </summary>
    public TipoDeVehiculo? TipoDeVehiculo { get; init; }
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

/// <summary>
/// Cuerpo de la solicitud que paga todo lo pendiente de una placa. Si falta un campo, la API responde 400 indicando cuál.
/// </summary>
public sealed class SolicitudDePagoDeLaPlacaHttp
{
    /// <summary>
    /// Placa del vehículo; los espacios y los guiones se quitan.
    /// </summary>
    [Required(ErrorMessage = "La placa es obligatoria")]
    public string? Placa { get; init; }

    /// <summary>
    /// Referencia única de la solicitud. El cliente la genera una vez por intento de pago y la repite en los reintentos.
    /// </summary>
    [Required(ErrorMessage = "La referencia es obligatoria")]
    public Guid? Referencia { get; init; }
}
