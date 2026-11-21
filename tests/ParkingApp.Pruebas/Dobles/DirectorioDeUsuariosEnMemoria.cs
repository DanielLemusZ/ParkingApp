using ParkingApp.Aplicacion.Identidad;

namespace ParkingApp.Pruebas.Dobles;

/// <summary>
/// Directorio de usuarios en memoria, para probar la creación de usuarios iniciales sin Identity ni base de datos.
/// Como Identity, compara los correos sin distinguir mayúsculas y rechaza contraseñas cortas.
/// </summary>
internal sealed class DirectorioDeUsuariosEnMemoria : IDirectorioDeUsuarios
{
    /// <summary>
    /// Largo mínimo de la contraseña, para simular la política de contraseñas de Identity.
    /// </summary>
    public const int LargoMinimoDeContrasena = 8;

    private readonly Dictionary<string, string> rolesPorCorreo = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyDictionary<string, string> RolesPorCorreo => rolesPorCorreo;

    public int VecesQueSeCreo { get; private set; }

    public Task<bool> ExisteAsync(string correo, CancellationToken cancelacion = default)
    {
        return Task.FromResult(rolesPorCorreo.ContainsKey(correo));
    }

    public Task<ResultadoDeCreacionDeUsuario> CrearAsync(string correo, string contrasena, string rol, CancellationToken cancelacion = default)
    {
        if (contrasena.Length < LargoMinimoDeContrasena)
        {
            return Task.FromResult(new ResultadoDeCreacionDeUsuario(false, "La contraseña es muy corta"));
        }

        rolesPorCorreo[correo] = rol;
        VecesQueSeCreo++;
        return Task.FromResult(new ResultadoDeCreacionDeUsuario(true, string.Empty));
    }
}
