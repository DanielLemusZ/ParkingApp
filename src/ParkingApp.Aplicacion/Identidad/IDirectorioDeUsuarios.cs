namespace ParkingApp.Aplicacion.Identidad;

/// <summary>
/// Puerto hacia donde se guardan los usuarios. La aplicación no sabe si detrás está ASP.NET Core Identity u otro
/// proveedor: la infraestructura lo implementa.
/// </summary>
public interface IDirectorioDeUsuarios
{
    /// <summary>
    /// Indica si ya existe un usuario con el correo.
    /// </summary>
    /// <param name="correo">Correo del usuario.</param>
    /// <param name="cancelacion">Permite cancelar la operación.</param>
    /// <returns><see langword="true"/> si el correo ya tiene un usuario.</returns>
    Task<bool> ExisteAsync(string correo, CancellationToken cancelacion = default);

    /// <summary>
    /// Crea un usuario con su contraseña y su rol. La contraseña se guarda solo como hash.
    /// </summary>
    /// <param name="correo">Correo, que también es el nombre de usuario.</param>
    /// <param name="contrasena">Contraseña en texto plano; no se guarda así.</param>
    /// <param name="rol">Rol del usuario, uno de <see cref="Roles.Todos"/>.</param>
    /// <param name="cancelacion">Permite cancelar la operación.</param>
    /// <returns>Si se creó, o el motivo del rechazo (por ejemplo, una contraseña que no cumple la política).</returns>
    Task<ResultadoDeCreacionDeUsuario> CrearAsync(string correo, string contrasena, string rol, CancellationToken cancelacion = default);

    /// <summary>
    /// Lista todos los usuarios con su rol y su bloqueo.
    /// </summary>
    /// <param name="cancelacion">Permite cancelar la operación.</param>
    /// <returns>Los usuarios, ordenados por correo.</returns>
    Task<IReadOnlyList<UsuarioDelDirectorio>> ListarAsync(CancellationToken cancelacion = default);

    /// <summary>
    /// Busca un usuario por su identificador.
    /// </summary>
    /// <param name="id">Identificador del usuario.</param>
    /// <param name="cancelacion">Permite cancelar la operación.</param>
    /// <returns>El usuario, o <see langword="null"/> si no existe.</returns>
    Task<UsuarioDelDirectorio?> ObtenerAsync(Guid id, CancellationToken cancelacion = default);

    /// <summary>
    /// Reemplaza el rol del usuario y cierra sus sesiones abiertas, para que el rol nuevo se aplique al volver a entrar.
    /// </summary>
    /// <param name="id">Identificador del usuario.</param>
    /// <param name="rol">Rol nuevo, uno de <see cref="Roles.Todos"/>.</param>
    /// <param name="cancelacion">Permite cancelar la operación.</param>
    /// <returns>Si se cambió, o el motivo del rechazo.</returns>
    Task<ResultadoDeCambioDeUsuario> CambiarRolAsync(Guid id, string rol, CancellationToken cancelacion = default);

    /// <summary>
    /// Pone una contraseña nueva al usuario, sin pedir la anterior. Sus sesiones abiertas se cierran.
    /// </summary>
    /// <param name="id">Identificador del usuario.</param>
    /// <param name="contrasena">Contraseña nueva en texto plano; no se guarda así.</param>
    /// <param name="cancelacion">Permite cancelar la operación.</param>
    /// <returns>Si se cambió, o el motivo del rechazo (por ejemplo, una contraseña que no cumple la política).</returns>
    Task<ResultadoDeCambioDeUsuario> RestablecerContrasenaAsync(Guid id, string contrasena, CancellationToken cancelacion = default);

    /// <summary>
    /// Desactiva al usuario: lo bloquea sin fecha de fin (<see cref="DateTimeOffset.MaxValue"/>) y cierra sus sesiones abiertas.
    /// </summary>
    /// <param name="id">Identificador del usuario.</param>
    /// <param name="cancelacion">Permite cancelar la operación.</param>
    /// <returns>Si se desactivó, o el motivo del rechazo.</returns>
    Task<ResultadoDeCambioDeUsuario> DesactivarAsync(Guid id, CancellationToken cancelacion = default);

    /// <summary>
    /// Quita el bloqueo del usuario, sea una desactivación o un bloqueo por intentos fallidos, y reinicia los intentos.
    /// </summary>
    /// <param name="id">Identificador del usuario.</param>
    /// <param name="cancelacion">Permite cancelar la operación.</param>
    /// <returns>Si se quitó, o el motivo del rechazo.</returns>
    Task<ResultadoDeCambioDeUsuario> QuitarBloqueoAsync(Guid id, CancellationToken cancelacion = default);

    /// <summary>
    /// Borra el usuario y su rol.
    /// </summary>
    /// <param name="id">Identificador del usuario.</param>
    /// <param name="cancelacion">Permite cancelar la operación.</param>
    /// <returns>Si se borró, o el motivo del rechazo.</returns>
    Task<ResultadoDeCambioDeUsuario> BorrarAsync(Guid id, CancellationToken cancelacion = default);
}

/// <summary>
/// Resultado de crear un usuario.
/// </summary>
/// <param name="Creado">Si el usuario quedó creado.</param>
/// <param name="Motivo">Por qué no se creó, o vacío si se creó.</param>
public sealed record ResultadoDeCreacionDeUsuario(bool Creado, string Motivo);

/// <summary>
/// Resultado de cambiar un usuario: su rol, su contraseña, su bloqueo, o borrarlo.
/// </summary>
/// <param name="Hecho">Si el cambio quedó hecho.</param>
/// <param name="Motivo">Por qué no se hizo, o vacío si se hizo.</param>
public sealed record ResultadoDeCambioDeUsuario(bool Hecho, string Motivo);

/// <summary>
/// Usuario tal como lo guarda el directorio.
/// </summary>
/// <param name="Id">Identificador del usuario.</param>
/// <param name="Correo">Correo, que también es el nombre de usuario y el operador que queda en los pagos.</param>
/// <param name="Rol">Rol del usuario, o vacío si no tiene.</param>
/// <param name="BloqueadoHasta">
/// Hasta cuándo está bloqueado, en UTC, o <see langword="null"/> si nunca se bloqueó. Un usuario desactivado tiene
/// <see cref="DateTimeOffset.MaxValue"/>.
/// </param>
public sealed record UsuarioDelDirectorio(Guid Id, string Correo, string Rol, DateTimeOffset? BloqueadoHasta);
