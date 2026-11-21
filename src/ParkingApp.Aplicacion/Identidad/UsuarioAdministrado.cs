namespace ParkingApp.Aplicacion.Identidad;

/// <summary>
/// Usuario como se muestra en la pantalla de usuarios y en su API.
/// </summary>
/// <param name="Id">Identificador del usuario.</param>
/// <param name="Correo">Correo del usuario; no se puede cambiar.</param>
/// <param name="Rol">Rol del usuario.</param>
/// <param name="Estado">Si puede iniciar sesión, está bloqueado por intentos fallidos o está desactivado.</param>
public sealed record UsuarioAdministrado(Guid Id, string Correo, string Rol, EstadoDeUsuario Estado);

/// <summary>
/// Estado de un usuario, calculado con la fecha de fin de su bloqueo.
/// </summary>
public enum EstadoDeUsuario
{
    /// <summary>
    /// Puede iniciar sesión.
    /// </summary>
    Activo,

    /// <summary>
    /// Bloqueado por intentos fallidos, hasta que pase el tiempo del bloqueo o el administrador lo desbloquee.
    /// </summary>
    Bloqueado,

    /// <summary>
    /// Desactivado por el administrador: no puede iniciar sesión hasta que lo reactiven.
    /// </summary>
    Desactivado,
}

/// <summary>
/// Respuesta de una acción sobre los usuarios: si se hizo y el texto que se muestra.
/// </summary>
/// <param name="Exito">Si la acción se hizo.</param>
/// <param name="Motivo">Texto que se muestra en la pantalla.</param>
public sealed record ResultadoDeAdministracion(bool Exito, string Motivo);
