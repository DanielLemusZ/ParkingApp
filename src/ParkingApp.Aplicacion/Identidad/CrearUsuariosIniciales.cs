namespace ParkingApp.Aplicacion.Identidad;

/// <summary>
/// Caso de uso del arranque (F-10): crea los usuarios iniciales de cada rol con las credenciales de <c>.env</c>,
/// solo si todavía no existen. Así no hay contraseñas en el repositorio, ni siquiera como hash en una migración.
/// </summary>
public sealed class CrearUsuariosIniciales
{
    private readonly IDirectorioDeUsuarios directorio;

    /// <summary>
    /// Crea el caso de uso con el directorio de usuarios.
    /// </summary>
    /// <param name="directorio">Donde se guardan los usuarios.</param>
    public CrearUsuariosIniciales(IDirectorioDeUsuarios directorio)
    {
        this.directorio = directorio;
    }

    /// <summary>
    /// Crea los usuarios iniciales que falten.
    /// </summary>
    /// <param name="usuarios">Usuarios leídos de la configuración; el correo o la contraseña pueden faltar.</param>
    /// <param name="cancelacion">Permite cancelar la operación.</param>
    /// <returns>Qué pasó con cada usuario, para dejarlo en el registro sin mostrar contraseñas.</returns>
    public async Task<IReadOnlyList<ResultadoDeUsuarioInicial>> EjecutarAsync(
        IEnumerable<UsuarioInicial> usuarios,
        CancellationToken cancelacion = default)
    {
        ArgumentNullException.ThrowIfNull(usuarios);

        var resultados = new List<ResultadoDeUsuarioInicial>();

        foreach (var usuario in usuarios)
        {
            resultados.Add(await CrearSiFaltaAsync(usuario, cancelacion));
        }

        return resultados;
    }

    private async Task<ResultadoDeUsuarioInicial> CrearSiFaltaAsync(UsuarioInicial usuario, CancellationToken cancelacion)
    {
        if (!Roles.Todos.Contains(usuario.Rol))
        {
            return new ResultadoDeUsuarioInicial(usuario.Correo, usuario.Rol, EstadoDeUsuarioInicial.Rechazado, $"El rol {usuario.Rol} no existe");
        }

        if (string.IsNullOrWhiteSpace(usuario.Correo) || string.IsNullOrWhiteSpace(usuario.Contrasena))
        {
            return new ResultadoDeUsuarioInicial(usuario.Correo, usuario.Rol, EstadoDeUsuarioInicial.SinCredenciales, "Faltan el correo o la contraseña en .env");
        }

        var correo = usuario.Correo.Trim();

        // Si ya existe no se toca: el usuario pudo haber cambiado su contraseña, y el arranque no debe pisarla.
        if (await directorio.ExisteAsync(correo, cancelacion))
        {
            return new ResultadoDeUsuarioInicial(correo, usuario.Rol, EstadoDeUsuarioInicial.YaExistia, string.Empty);
        }

        var creacion = await directorio.CrearAsync(correo, usuario.Contrasena, usuario.Rol, cancelacion);
        var estado = creacion.Creado ? EstadoDeUsuarioInicial.Creado : EstadoDeUsuarioInicial.Rechazado;

        return new ResultadoDeUsuarioInicial(correo, usuario.Rol, estado, creacion.Motivo);
    }
}

/// <summary>
/// Usuario inicial leído de la configuración.
/// </summary>
/// <param name="Correo">Correo del usuario, o nada si falta en <c>.env</c>.</param>
/// <param name="Contrasena">Contraseña, o nada si falta en <c>.env</c>.</param>
/// <param name="Rol">Rol que se le asigna.</param>
public sealed record UsuarioInicial(string? Correo, string? Contrasena, string Rol)
{
    /// <summary>
    /// Se escribe sin la contraseña, para que nunca termine en el registro por accidente.
    /// </summary>
    /// <returns>El correo y el rol.</returns>
    public override string ToString()
    {
        return $"{Correo} ({Rol})";
    }
}

/// <summary>
/// Qué pasó con un usuario inicial al arrancar.
/// </summary>
/// <param name="Correo">Correo del usuario.</param>
/// <param name="Rol">Rol del usuario.</param>
/// <param name="Estado">Resultado.</param>
/// <param name="Motivo">Detalle cuando no se creó.</param>
public sealed record ResultadoDeUsuarioInicial(string? Correo, string Rol, EstadoDeUsuarioInicial Estado, string Motivo);

/// <summary>
/// Resultado de crear un usuario inicial.
/// </summary>
public enum EstadoDeUsuarioInicial
{
    /// <summary>
    /// Se creó en este arranque.
    /// </summary>
    Creado,

    /// <summary>
    /// Ya existía y no se cambió.
    /// </summary>
    YaExistia,

    /// <summary>
    /// Faltan el correo o la contraseña en <c>.env</c>.
    /// </summary>
    SinCredenciales,

    /// <summary>
    /// No se pudo crear, por ejemplo porque la contraseña no cumple la política o el rol no existe.
    /// </summary>
    Rechazado,
}
