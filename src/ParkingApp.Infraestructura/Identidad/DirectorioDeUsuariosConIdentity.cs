using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

using ParkingApp.Aplicacion.Identidad;

namespace ParkingApp.Infraestructura.Identidad;

/// <summary>
/// Adaptador del directorio de usuarios sobre ASP.NET Core Identity. <see cref="UserManager{TUser}"/> se encarga
/// del hash de la contraseña, de validar la política de contraseñas, de asignar el rol y del bloqueo.
/// </summary>
internal sealed class DirectorioDeUsuariosConIdentity : IDirectorioDeUsuarios
{
    private readonly UserManager<Usuario> usuarios;

    public DirectorioDeUsuariosConIdentity(UserManager<Usuario> usuarios)
    {
        this.usuarios = usuarios;
    }

    public async Task<bool> ExisteAsync(string correo, CancellationToken cancelacion = default)
    {
        // UserManager no recibe un token de cancelación: se revisa antes de llamarlo.
        cancelacion.ThrowIfCancellationRequested();

        // FindByEmailAsync busca por el correo normalizado (en mayúsculas), así que no distingue mayúsculas.
        return await usuarios.FindByEmailAsync(correo) is not null;
    }

    public async Task<ResultadoDeCreacionDeUsuario> CrearAsync(string correo, string contrasena, string rol, CancellationToken cancelacion = default)
    {
        cancelacion.ThrowIfCancellationRequested();

        // El correo también es el nombre de usuario. Se marca como confirmado porque lo crea el sistema,
        // no un registro público.
        var usuario = new Usuario
        {
            UserName = correo,
            Email = correo,
            EmailConfirmed = true,
        };

        // CreateAsync valida la contraseña contra la política y guarda solo su hash (PBKDF2 con sal).
        var creacion = await usuarios.CreateAsync(usuario, contrasena);

        if (!creacion.Succeeded)
        {
            return Rechazo(creacion);
        }

        var asignacion = await usuarios.AddToRoleAsync(usuario, rol);

        if (!asignacion.Succeeded)
        {
            // Un usuario sin rol no podría usar nada: se borra para que el próximo arranque lo intente de nuevo.
            await usuarios.DeleteAsync(usuario);
            return Rechazo(asignacion);
        }

        return new ResultadoDeCreacionDeUsuario(true, string.Empty);
    }

    public async Task<IReadOnlyList<UsuarioDelDirectorio>> ListarAsync(CancellationToken cancelacion = default)
    {
        // Users es una consulta de EF Core sobre la tabla de usuarios. Una sede tiene pocos usuarios, así que el rol
        // de cada uno se busca por separado.
        var todos = await usuarios.Users
            .AsNoTracking()
            .OrderBy(usuario => usuario.Email)
            .ToListAsync(cancelacion);

        var lista = new List<UsuarioDelDirectorio>();

        foreach (var usuario in todos)
        {
            lista.Add(await ADatosDelDirectorioAsync(usuario));
        }

        return lista;
    }

    public async Task<UsuarioDelDirectorio?> ObtenerAsync(Guid id, CancellationToken cancelacion = default)
    {
        var usuario = await BuscarAsync(id, cancelacion);

        if (usuario is null)
        {
            return null;
        }

        return await ADatosDelDirectorioAsync(usuario);
    }

    public async Task<ResultadoDeCambioDeUsuario> CambiarRolAsync(Guid id, string rol, CancellationToken cancelacion = default)
    {
        var usuario = await BuscarAsync(id, cancelacion);

        if (usuario is null)
        {
            return NoEncontrado();
        }

        var actuales = await usuarios.GetRolesAsync(usuario);

        if (actuales.Count > 0)
        {
            var retiro = await usuarios.RemoveFromRolesAsync(usuario, actuales);

            if (!retiro.Succeeded)
            {
                return Cambio(retiro);
            }
        }

        var asignacion = await usuarios.AddToRoleAsync(usuario, rol);

        if (!asignacion.Succeeded)
        {
            return Cambio(asignacion);
        }

        // Los roles viajan en la cookie. Con un sello de seguridad nuevo, la sesión abierta deja de valer y el rol nuevo
        // se aplica cuando el usuario vuelve a entrar.
        return Cambio(await usuarios.UpdateSecurityStampAsync(usuario));
    }

    public async Task<ResultadoDeCambioDeUsuario> RestablecerContrasenaAsync(Guid id, string contrasena, CancellationToken cancelacion = default)
    {
        var usuario = await BuscarAsync(id, cancelacion);

        if (usuario is null)
        {
            return NoEncontrado();
        }

        // El token prueba que el cambio lo autoriza el sistema. ResetPasswordAsync valida la política, guarda el hash
        // nuevo y cambia el sello de seguridad, así que las sesiones abiertas con la contraseña vieja se cierran.
        var token = await usuarios.GeneratePasswordResetTokenAsync(usuario);
        return Cambio(await usuarios.ResetPasswordAsync(usuario, token, contrasena));
    }

    public async Task<ResultadoDeCambioDeUsuario> DesactivarAsync(Guid id, CancellationToken cancelacion = default)
    {
        var usuario = await BuscarAsync(id, cancelacion);

        if (usuario is null)
        {
            return NoEncontrado();
        }

        // El bloqueo solo aplica si está habilitado para el usuario. Los usuarios nuevos lo traen (AllowedForNewUsers).
        if (!usuario.LockoutEnabled)
        {
            var habilitacion = await usuarios.SetLockoutEnabledAsync(usuario, true);

            if (!habilitacion.Succeeded)
            {
                return Cambio(habilitacion);
            }
        }

        // Desactivar es un bloqueo sin fecha de fin. Identity ya rechaza el inicio de sesión de un usuario bloqueado.
        var bloqueo = await usuarios.SetLockoutEndDateAsync(usuario, DateTimeOffset.MaxValue);

        if (!bloqueo.Succeeded)
        {
            return Cambio(bloqueo);
        }

        // Con un sello de seguridad nuevo, la cookie de una sesión abierta deja de valer (criterio 6 del #68).
        return Cambio(await usuarios.UpdateSecurityStampAsync(usuario));
    }

    public async Task<ResultadoDeCambioDeUsuario> QuitarBloqueoAsync(Guid id, CancellationToken cancelacion = default)
    {
        var usuario = await BuscarAsync(id, cancelacion);

        if (usuario is null)
        {
            return NoEncontrado();
        }

        var desbloqueo = await usuarios.SetLockoutEndDateAsync(usuario, null);

        if (!desbloqueo.Succeeded)
        {
            return Cambio(desbloqueo);
        }

        // Los intentos fallidos vuelven a cero: si no, el siguiente error lo bloquearía de nuevo.
        return Cambio(await usuarios.ResetAccessFailedCountAsync(usuario));
    }

    public async Task<ResultadoDeCambioDeUsuario> BorrarAsync(Guid id, CancellationToken cancelacion = default)
    {
        var usuario = await BuscarAsync(id, cancelacion);

        if (usuario is null)
        {
            return NoEncontrado();
        }

        // DeleteAsync borra también su rol.
        return Cambio(await usuarios.DeleteAsync(usuario));
    }

    private async Task<Usuario?> BuscarAsync(Guid id, CancellationToken cancelacion)
    {
        cancelacion.ThrowIfCancellationRequested();
        return await usuarios.FindByIdAsync(id.ToString());
    }

    private async Task<UsuarioDelDirectorio> ADatosDelDirectorioAsync(Usuario usuario)
    {
        var roles = await usuarios.GetRolesAsync(usuario);
        var rol = roles.Count > 0 ? roles[0] : string.Empty;
        var correo = usuario.Email ?? usuario.UserName ?? string.Empty;

        return new UsuarioDelDirectorio(usuario.Id, correo, rol, usuario.LockoutEnd);
    }

    private static ResultadoDeCreacionDeUsuario Rechazo(IdentityResult resultado)
    {
        return new ResultadoDeCreacionDeUsuario(false, Errores(resultado));
    }

    private static ResultadoDeCambioDeUsuario Cambio(IdentityResult resultado)
    {
        return resultado.Succeeded
            ? new ResultadoDeCambioDeUsuario(true, string.Empty)
            : new ResultadoDeCambioDeUsuario(false, Errores(resultado));
    }

    private static ResultadoDeCambioDeUsuario NoEncontrado()
    {
        return new ResultadoDeCambioDeUsuario(false, AdministrarUsuarios.UsuarioNoEncontrado);
    }

    private static string Errores(IdentityResult resultado)
    {
        return string.Join(" ", resultado.Errors.Select(error => error.Description));
    }
}
