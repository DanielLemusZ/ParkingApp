using ParkingApp.Aplicacion.Identidad;

namespace ParkingApp.Pruebas.Dobles;

/// <summary>
/// Directorio de usuarios en memoria, para probar los casos de uso de usuarios sin Identity ni base de datos.
/// Como Identity, compara los correos sin distinguir mayúsculas, rechaza contraseñas cortas y desactiva bloqueando
/// hasta la fecha máxima.
/// </summary>
internal sealed class DirectorioDeUsuariosEnMemoria : IDirectorioDeUsuarios
{
    /// <summary>
    /// Largo mínimo de la contraseña, para simular la política de contraseñas de Identity.
    /// </summary>
    public const int LargoMinimoDeContrasena = 8;

    /// <summary>
    /// Motivo con el que se rechaza una contraseña corta, para simular los errores de Identity.
    /// </summary>
    public const string ContrasenaMuyCorta = "La contraseña es muy corta";

    private readonly List<UsuarioGuardado> usuarios = new List<UsuarioGuardado>();

    public IReadOnlyDictionary<string, string> RolesPorCorreo
    {
        get
        {
            var roles = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            foreach (var usuario in usuarios)
            {
                roles[usuario.Correo] = usuario.Rol;
            }

            return roles;
        }
    }

    public int VecesQueSeCreo { get; private set; }

    /// <summary>
    /// Guarda un usuario ya existente, para preparar una prueba.
    /// </summary>
    public Guid Agregar(string correo, string rol)
    {
        var usuario = new UsuarioGuardado(correo, rol, "Clave-Inicial-1");
        usuarios.Add(usuario);
        return usuario.Id;
    }

    /// <summary>
    /// Simula el bloqueo que Identity aplica tras 5 intentos fallidos.
    /// </summary>
    public void BloquearHasta(string correo, DateTimeOffset hasta)
    {
        Guardado(correo).BloqueadoHasta = hasta;
    }

    public DateTimeOffset? BloqueadoHastaDe(string correo)
    {
        return Guardado(correo).BloqueadoHasta;
    }

    public string ContrasenaDe(string correo)
    {
        return Guardado(correo).Contrasena;
    }

    public Task<bool> ExisteAsync(string correo, CancellationToken cancelacion = default)
    {
        return Task.FromResult(BuscarPorCorreo(correo) is not null);
    }

    public Task<ResultadoDeCreacionDeUsuario> CrearAsync(string correo, string contrasena, string rol, CancellationToken cancelacion = default)
    {
        if (contrasena.Length < LargoMinimoDeContrasena)
        {
            return Task.FromResult(new ResultadoDeCreacionDeUsuario(false, ContrasenaMuyCorta));
        }

        usuarios.Add(new UsuarioGuardado(correo, rol, contrasena));
        VecesQueSeCreo++;
        return Task.FromResult(new ResultadoDeCreacionDeUsuario(true, string.Empty));
    }

    public Task<IReadOnlyList<UsuarioDelDirectorio>> ListarAsync(CancellationToken cancelacion = default)
    {
        var lista = new List<UsuarioDelDirectorio>();

        foreach (var usuario in usuarios)
        {
            lista.Add(usuario.ADatos());
        }

        lista.Sort((uno, otro) => string.Compare(uno.Correo, otro.Correo, StringComparison.OrdinalIgnoreCase));
        return Task.FromResult<IReadOnlyList<UsuarioDelDirectorio>>(lista);
    }

    public Task<UsuarioDelDirectorio?> ObtenerAsync(Guid id, CancellationToken cancelacion = default)
    {
        var usuario = BuscarPorId(id);
        return Task.FromResult(usuario?.ADatos());
    }

    public Task<ResultadoDeCambioDeUsuario> CambiarRolAsync(Guid id, string rol, CancellationToken cancelacion = default)
    {
        var usuario = BuscarPorId(id);

        if (usuario is null)
        {
            return NoEncontrado();
        }

        usuario.Rol = rol;
        return Hecho();
    }

    public Task<ResultadoDeCambioDeUsuario> RestablecerContrasenaAsync(Guid id, string contrasena, CancellationToken cancelacion = default)
    {
        var usuario = BuscarPorId(id);

        if (usuario is null)
        {
            return NoEncontrado();
        }

        if (contrasena.Length < LargoMinimoDeContrasena)
        {
            return Task.FromResult(new ResultadoDeCambioDeUsuario(false, ContrasenaMuyCorta));
        }

        usuario.Contrasena = contrasena;
        return Hecho();
    }

    public Task<ResultadoDeCambioDeUsuario> DesactivarAsync(Guid id, CancellationToken cancelacion = default)
    {
        var usuario = BuscarPorId(id);

        if (usuario is null)
        {
            return NoEncontrado();
        }

        usuario.BloqueadoHasta = DateTimeOffset.MaxValue;
        return Hecho();
    }

    public Task<ResultadoDeCambioDeUsuario> QuitarBloqueoAsync(Guid id, CancellationToken cancelacion = default)
    {
        var usuario = BuscarPorId(id);

        if (usuario is null)
        {
            return NoEncontrado();
        }

        usuario.BloqueadoHasta = null;
        return Hecho();
    }

    public Task<ResultadoDeCambioDeUsuario> BorrarAsync(Guid id, CancellationToken cancelacion = default)
    {
        var usuario = BuscarPorId(id);

        if (usuario is null)
        {
            return NoEncontrado();
        }

        usuarios.Remove(usuario);
        return Hecho();
    }

    private UsuarioGuardado? BuscarPorCorreo(string correo)
    {
        foreach (var usuario in usuarios)
        {
            if (string.Equals(usuario.Correo, correo.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                return usuario;
            }
        }

        return null;
    }

    private UsuarioGuardado? BuscarPorId(Guid id)
    {
        foreach (var usuario in usuarios)
        {
            if (usuario.Id == id)
            {
                return usuario;
            }
        }

        return null;
    }

    private UsuarioGuardado Guardado(string correo)
    {
        return BuscarPorCorreo(correo) ?? throw new InvalidOperationException($"No hay un usuario {correo} en el directorio.");
    }

    private static Task<ResultadoDeCambioDeUsuario> Hecho()
    {
        return Task.FromResult(new ResultadoDeCambioDeUsuario(true, string.Empty));
    }

    private static Task<ResultadoDeCambioDeUsuario> NoEncontrado()
    {
        return Task.FromResult(new ResultadoDeCambioDeUsuario(false, AdministrarUsuarios.UsuarioNoEncontrado));
    }

    private sealed class UsuarioGuardado
    {
        public UsuarioGuardado(string correo, string rol, string contrasena)
        {
            Id = Guid.CreateVersion7();
            Correo = correo;
            Rol = rol;
            Contrasena = contrasena;
        }

        public Guid Id { get; }

        public string Correo { get; }

        public string Rol { get; set; }

        public string Contrasena { get; set; }

        public DateTimeOffset? BloqueadoHasta { get; set; }

        public UsuarioDelDirectorio ADatos()
        {
            return new UsuarioDelDirectorio(Id, Correo, Rol, BloqueadoHasta);
        }
    }
}
