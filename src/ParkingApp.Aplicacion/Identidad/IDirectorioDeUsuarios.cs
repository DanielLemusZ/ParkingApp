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
}

/// <summary>
/// Resultado de crear un usuario.
/// </summary>
/// <param name="Creado">Si el usuario quedó creado.</param>
/// <param name="Motivo">Por qué no se creó, o vacío si se creó.</param>
public sealed record ResultadoDeCreacionDeUsuario(bool Creado, string Motivo);
