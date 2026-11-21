using Microsoft.AspNetCore.Identity;

using ParkingApp.Aplicacion.Identidad;

namespace ParkingApp.Infraestructura.Identidad;

/// <summary>
/// Adaptador del directorio de usuarios sobre ASP.NET Core Identity. <see cref="UserManager{TUser}"/> se encarga
/// del hash de la contraseña, de validar la política de contraseñas y de asignar el rol.
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

    private static ResultadoDeCreacionDeUsuario Rechazo(IdentityResult resultado)
    {
        var motivo = string.Join(" ", resultado.Errors.Select(error => error.Description));
        return new ResultadoDeCreacionDeUsuario(false, motivo);
    }
}
