using System.ComponentModel.DataAnnotations;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using ParkingApp.Aplicacion.Identidad;

namespace ParkingApp.Web.Controllers;

/// <summary>
/// API de administración de usuarios (#68). Solo la usa el Administrador: sin sesión responde 401 y con el rol
/// Operador responde 403. Una acción que las reglas rechazan responde 200 con <c>Exito</c> en falso y el motivo.
/// </summary>
[Authorize(Roles = Roles.Administrador)]
[ApiController]
[Route("api/usuarios")]
public sealed class UsuariosController : ControllerBase
{
    private readonly AdministrarUsuarios administrarUsuarios;

    /// <summary>
    /// Crea el controlador con el caso de uso de los usuarios, el mismo que usa la pantalla.
    /// </summary>
    /// <param name="administrarUsuarios">Caso de uso de la administración de usuarios.</param>
    public UsuariosController(AdministrarUsuarios administrarUsuarios)
    {
        this.administrarUsuarios = administrarUsuarios;
    }

    /// <summary>
    /// Lista los usuarios con su correo, rol y estado.
    /// </summary>
    /// <param name="correo">Parte del correo que se busca; vacío para listar todos.</param>
    /// <param name="cancelacion">Se cancela si el cliente cierra la conexión.</param>
    /// <returns>Los usuarios, ordenados por correo.</returns>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<UsuarioAdministrado>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IReadOnlyList<UsuarioAdministrado>>> ListarAsync(
        [FromQuery] string? correo,
        CancellationToken cancelacion)
    {
        var usuarios = await administrarUsuarios.ListarAsync(correo, cancelacion);
        return Ok(usuarios);
    }

    /// <summary>
    /// Crea un usuario con su rol y una contraseña inicial que cumpla la política.
    /// </summary>
    /// <param name="solicitud">Correo, rol y contraseña inicial.</param>
    /// <param name="cancelacion">Se cancela si el cliente cierra la conexión.</param>
    /// <returns>Si se creó y el motivo.</returns>
    [HttpPost]
    [ProducesResponseType<ResultadoDeAdministracion>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ResultadoDeAdministracion>> CrearAsync(
        SolicitudDeUsuarioNuevoHttp solicitud,
        CancellationToken cancelacion)
    {
        ArgumentNullException.ThrowIfNull(solicitud);
        return await administrarUsuarios.CrearAsync(solicitud.Correo, solicitud.Rol, solicitud.Contrasena, cancelacion);
    }

    /// <summary>
    /// Cambia el rol de un usuario. El correo no se puede cambiar. El administrador no puede cambiarse el rol a sí mismo.
    /// </summary>
    /// <param name="id">Identificador del usuario.</param>
    /// <param name="solicitud">Rol nuevo.</param>
    /// <param name="cancelacion">Se cancela si el cliente cierra la conexión.</param>
    /// <returns>Si se cambió y el motivo.</returns>
    [HttpPut("{id:guid}/rol")]
    [ProducesResponseType<ResultadoDeAdministracion>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ResultadoDeAdministracion>> CambiarRolAsync(
        Guid id,
        SolicitudDeRolHttp solicitud,
        CancellationToken cancelacion)
    {
        ArgumentNullException.ThrowIfNull(solicitud);
        return await administrarUsuarios.CambiarRolAsync(id, solicitud.Rol, CorreoDeLaSesion(), cancelacion);
    }

    /// <summary>
    /// Restablece la contraseña de un usuario que la olvidó.
    /// </summary>
    /// <param name="id">Identificador del usuario.</param>
    /// <param name="solicitud">Contraseña nueva, que debe cumplir la política.</param>
    /// <param name="cancelacion">Se cancela si el cliente cierra la conexión.</param>
    /// <returns>Si se cambió y el motivo.</returns>
    [HttpPut("{id:guid}/contrasena")]
    [ProducesResponseType<ResultadoDeAdministracion>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ResultadoDeAdministracion>> RestablecerContrasenaAsync(
        Guid id,
        SolicitudDeContrasenaHttp solicitud,
        CancellationToken cancelacion)
    {
        ArgumentNullException.ThrowIfNull(solicitud);
        return await administrarUsuarios.RestablecerContrasenaAsync(id, solicitud.Contrasena, cancelacion);
    }

    /// <summary>
    /// Desactiva un usuario: no puede iniciar sesión, y su sesión abierta se cierra.
    /// </summary>
    /// <param name="id">Identificador del usuario.</param>
    /// <param name="cancelacion">Se cancela si el cliente cierra la conexión.</param>
    /// <returns>Si se desactivó y el motivo.</returns>
    [HttpPost("{id:guid}/desactivar")]
    [ProducesResponseType<ResultadoDeAdministracion>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ResultadoDeAdministracion>> DesactivarAsync(Guid id, CancellationToken cancelacion)
    {
        return await administrarUsuarios.DesactivarAsync(id, CorreoDeLaSesion(), cancelacion);
    }

    /// <summary>
    /// Reactiva un usuario desactivado.
    /// </summary>
    /// <param name="id">Identificador del usuario.</param>
    /// <param name="cancelacion">Se cancela si el cliente cierra la conexión.</param>
    /// <returns>Si se reactivó y el motivo.</returns>
    [HttpPost("{id:guid}/reactivar")]
    [ProducesResponseType<ResultadoDeAdministracion>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ResultadoDeAdministracion>> ReactivarAsync(Guid id, CancellationToken cancelacion)
    {
        return await administrarUsuarios.ReactivarAsync(id, cancelacion);
    }

    /// <summary>
    /// Desbloquea un usuario bloqueado por intentos fallidos.
    /// </summary>
    /// <param name="id">Identificador del usuario.</param>
    /// <param name="cancelacion">Se cancela si el cliente cierra la conexión.</param>
    /// <returns>Si se desbloqueó y el motivo.</returns>
    [HttpPost("{id:guid}/desbloquear")]
    [ProducesResponseType<ResultadoDeAdministracion>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ResultadoDeAdministracion>> DesbloquearAsync(Guid id, CancellationToken cancelacion)
    {
        return await administrarUsuarios.DesbloquearAsync(id, cancelacion);
    }

    /// <summary>
    /// Borra un usuario que nunca registró un pago. Si registró pagos, responde que hay que desactivarlo.
    /// </summary>
    /// <param name="id">Identificador del usuario.</param>
    /// <param name="cancelacion">Se cancela si el cliente cierra la conexión.</param>
    /// <returns>Si se borró y el motivo.</returns>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType<ResultadoDeAdministracion>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ResultadoDeAdministracion>> BorrarAsync(Guid id, CancellationToken cancelacion)
    {
        return await administrarUsuarios.BorrarAsync(id, CorreoDeLaSesion(), cancelacion);
    }

    // El administrador que hace la acción. [Authorize] garantiza que hay uno.
    private string CorreoDeLaSesion()
    {
        return User.Identity?.Name ?? throw new InvalidOperationException("La solicitud no tiene un usuario con sesión.");
    }
}

/// <summary>
/// Cuerpo de la solicitud para crear un usuario. Si falta un campo, la API responde 400 indicando cuál.
/// </summary>
public sealed class SolicitudDeUsuarioNuevoHttp
{
    /// <summary>
    /// Correo del usuario; también es su nombre de usuario.
    /// </summary>
    [Required(ErrorMessage = "El correo es obligatorio")]
    public string? Correo { get; init; }

    /// <summary>
    /// Rol del usuario: <c>Operador</c> o <c>Administrador</c>.
    /// </summary>
    [Required(ErrorMessage = "El rol es obligatorio")]
    public string? Rol { get; init; }

    /// <summary>
    /// Contraseña inicial: al menos 8 caracteres, con una mayúscula, una minúscula, un número y un símbolo.
    /// </summary>
    [Required(ErrorMessage = "La contraseña es obligatoria")]
    public string? Contrasena { get; init; }
}

/// <summary>
/// Cuerpo de la solicitud para cambiar el rol de un usuario.
/// </summary>
public sealed class SolicitudDeRolHttp
{
    /// <summary>
    /// Rol nuevo: <c>Operador</c> o <c>Administrador</c>.
    /// </summary>
    [Required(ErrorMessage = "El rol es obligatorio")]
    public string? Rol { get; init; }
}

/// <summary>
/// Cuerpo de la solicitud para restablecer la contraseña de un usuario.
/// </summary>
public sealed class SolicitudDeContrasenaHttp
{
    /// <summary>
    /// Contraseña nueva: al menos 8 caracteres, con una mayúscula, una minúscula, un número y un símbolo.
    /// </summary>
    [Required(ErrorMessage = "La contraseña es obligatoria")]
    public string? Contrasena { get; init; }
}
