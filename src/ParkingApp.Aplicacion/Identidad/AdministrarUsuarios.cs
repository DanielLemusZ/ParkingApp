using ParkingApp.Aplicacion.Pagos;

namespace ParkingApp.Aplicacion.Identidad;

/// <summary>
/// Caso de uso del Administrador (#68): lista, crea, cambia el rol, restablece la contraseña, desactiva, reactiva,
/// desbloquea y borra usuarios. Siempre queda un Administrador activo, porque nadie puede desactivarse, borrarse ni
/// cambiarse el rol a sí mismo; y un usuario con pagos no se borra. Lo usan la pantalla de usuarios y la API.
/// </summary>
public sealed class AdministrarUsuarios
{
    /// <summary>
    /// Respuesta cuando el usuario no existe.
    /// </summary>
    public const string UsuarioNoEncontrado = "Usuario no encontrado";

    /// <summary>
    /// Respuesta al intentar borrar un usuario que registró pagos: los pagos guardan su correo como operador.
    /// </summary>
    public const string TienePagos = "El usuario tiene pagos registrados: desactívalo en lugar de borrarlo";

    /// <summary>
    /// Respuesta al desbloquear un usuario que no está bloqueado por intentos fallidos.
    /// </summary>
    public const string NoEstaBloqueado = "El usuario no está bloqueado";

    /// <summary>
    /// Respuesta al reactivar un usuario que no está desactivado.
    /// </summary>
    public const string NoEstaDesactivado = "El usuario no está desactivado";

    /// <summary>
    /// Respuesta al desactivar un usuario que ya está desactivado.
    /// </summary>
    public const string YaEstaDesactivado = "El usuario ya está desactivado";

    /// <summary>
    /// Respuesta cuando el administrador intenta desactivarse a sí mismo.
    /// </summary>
    public const string NoPuedeDesactivarse = "No puedes desactivar tu propio usuario: siempre debe quedar un Administrador activo";

    /// <summary>
    /// Respuesta cuando el administrador intenta borrarse a sí mismo.
    /// </summary>
    public const string NoPuedeBorrarse = "No puedes borrar tu propio usuario: siempre debe quedar un Administrador activo";

    /// <summary>
    /// Respuesta cuando el administrador intenta cambiarse el rol a sí mismo.
    /// </summary>
    public const string NoPuedeCambiarseElRol = "No puedes cambiar tu propio rol: siempre debe quedar un Administrador activo";

    /// <summary>
    /// Respuesta cuando falta el correo.
    /// </summary>
    public const string FaltaElCorreo = "Escribe el correo";

    /// <summary>
    /// Respuesta cuando falta la contraseña.
    /// </summary>
    public const string FaltaLaContrasena = "Escribe la contraseña";

    private readonly IDirectorioDeUsuarios directorio;
    private readonly IRepositorioPagos pagos;
    private readonly TimeProvider reloj;

    /// <summary>
    /// Crea el caso de uso con sus dependencias.
    /// </summary>
    /// <param name="directorio">Donde se guardan los usuarios.</param>
    /// <param name="pagos">Donde se consulta si un usuario registró pagos.</param>
    /// <param name="reloj">Reloj del sistema, para saber si un bloqueo ya venció; en las pruebas, uno falso.</param>
    public AdministrarUsuarios(IDirectorioDeUsuarios directorio, IRepositorioPagos pagos, TimeProvider reloj)
    {
        this.directorio = directorio;
        this.pagos = pagos;
        this.reloj = reloj;
    }

    /// <summary>
    /// Respuesta al crear un usuario con un correo que ya tiene otro, por ejemplo
    /// <c>Ya existe un usuario con el correo operador@parkingapp.local</c>.
    /// </summary>
    /// <param name="correo">Correo repetido.</param>
    /// <returns>El texto que se muestra en la pantalla.</returns>
    public static string CorreoRepetido(string correo)
    {
        return $"Ya existe un usuario con el correo {correo}";
    }

    /// <summary>
    /// Respuesta cuando el rol no es Operador ni Administrador.
    /// </summary>
    /// <param name="rol">Rol que llegó.</param>
    /// <returns>El texto que se muestra en la pantalla.</returns>
    public static string RolInexistente(string? rol)
    {
        return $"El rol {rol} no existe";
    }

    /// <summary>
    /// Indica si un bloqueo es una desactivación: el directorio desactiva bloqueando hasta la fecha máxima.
    /// </summary>
    /// <param name="bloqueadoHasta">Fin del bloqueo, o <see langword="null"/> si no hay.</param>
    /// <returns><see langword="true"/> si el usuario está desactivado.</returns>
    public static bool EstaDesactivado(DateTimeOffset? bloqueadoHasta)
    {
        // Se compara el año y no la fecha exacta, porque la base de datos guarda la fecha con menos precisión.
        return bloqueadoHasta is not null && bloqueadoHasta.Value.Year == DateTimeOffset.MaxValue.Year;
    }

    /// <summary>
    /// Calcula el estado de un usuario con la fecha de fin de su bloqueo.
    /// </summary>
    /// <param name="bloqueadoHasta">Fin del bloqueo, o <see langword="null"/> si no hay.</param>
    /// <param name="ahora">Fecha y hora actual, en UTC.</param>
    /// <returns>Desactivado, Bloqueado si el bloqueo no ha vencido, o Activo.</returns>
    public static EstadoDeUsuario EstadoSegunElBloqueo(DateTimeOffset? bloqueadoHasta, DateTimeOffset ahora)
    {
        if (EstaDesactivado(bloqueadoHasta))
        {
            return EstadoDeUsuario.Desactivado;
        }

        if (bloqueadoHasta is not null && bloqueadoHasta.Value > ahora)
        {
            return EstadoDeUsuario.Bloqueado;
        }

        return EstadoDeUsuario.Activo;
    }

    /// <summary>
    /// Lista los usuarios cuyo correo contiene el texto buscado, sin distinguir mayúsculas.
    /// </summary>
    /// <param name="correo">Parte del correo; vacío para listar todos.</param>
    /// <param name="cancelacion">Permite cancelar la operación.</param>
    /// <returns>Los usuarios con su rol y su estado, ordenados por correo.</returns>
    public async Task<IReadOnlyList<UsuarioAdministrado>> ListarAsync(string? correo, CancellationToken cancelacion = default)
    {
        var busqueda = correo?.Trim() ?? string.Empty;
        var ahora = reloj.GetUtcNow();
        var usuarios = await directorio.ListarAsync(cancelacion);
        var lista = new List<UsuarioAdministrado>();

        foreach (var usuario in usuarios)
        {
            if (usuario.Correo.Contains(busqueda, StringComparison.OrdinalIgnoreCase))
            {
                var estado = EstadoSegunElBloqueo(usuario.BloqueadoHasta, ahora);
                lista.Add(new UsuarioAdministrado(usuario.Id, usuario.Correo, usuario.Rol, estado));
            }
        }

        return lista;
    }

    /// <summary>
    /// Crea un usuario con su rol y una contraseña inicial que cumpla la política.
    /// </summary>
    /// <param name="correo">Correo, que también es el nombre de usuario.</param>
    /// <param name="rol">Operador o Administrador.</param>
    /// <param name="contrasena">Contraseña inicial.</param>
    /// <param name="cancelacion">Permite cancelar la operación.</param>
    /// <returns>Si se creó y el texto que se muestra.</returns>
    public async Task<ResultadoDeAdministracion> CrearAsync(
        string? correo,
        string? rol,
        string? contrasena,
        CancellationToken cancelacion = default)
    {
        if (string.IsNullOrWhiteSpace(correo))
        {
            return Rechazar(FaltaElCorreo);
        }

        if (string.IsNullOrWhiteSpace(contrasena))
        {
            return Rechazar(FaltaLaContrasena);
        }

        if (rol is null || !Roles.Todos.Contains(rol))
        {
            return Rechazar(RolInexistente(rol));
        }

        var correoLimpio = correo.Trim();

        // Se revisa antes de crear para responder con el correo repetido, y no con los errores del directorio.
        if (await directorio.ExisteAsync(correoLimpio, cancelacion))
        {
            return Rechazar(CorreoRepetido(correoLimpio));
        }

        // La política de contraseñas la revisa el directorio, que conoce sus reglas.
        var creacion = await directorio.CrearAsync(correoLimpio, contrasena, rol, cancelacion);

        if (!creacion.Creado)
        {
            return Rechazar(creacion.Motivo);
        }

        return Hecho($"Usuario creado: {correoLimpio} ({rol})");
    }

    /// <summary>
    /// Cambia el rol de un usuario. El correo no se cambia, porque identifica al operador en los pagos que registró.
    /// </summary>
    /// <param name="id">Usuario que cambia de rol.</param>
    /// <param name="rol">Rol nuevo.</param>
    /// <param name="correoDeLaSesion">Correo del administrador que hace el cambio.</param>
    /// <param name="cancelacion">Permite cancelar la operación.</param>
    /// <returns>Si se cambió y el texto que se muestra.</returns>
    public async Task<ResultadoDeAdministracion> CambiarRolAsync(
        Guid id,
        string? rol,
        string correoDeLaSesion,
        CancellationToken cancelacion = default)
    {
        if (rol is null || !Roles.Todos.Contains(rol))
        {
            return Rechazar(RolInexistente(rol));
        }

        var usuario = await directorio.ObtenerAsync(id, cancelacion);

        if (usuario is null)
        {
            return Rechazar(UsuarioNoEncontrado);
        }

        if (EsElDeLaSesion(usuario, correoDeLaSesion))
        {
            return Rechazar(NoPuedeCambiarseElRol);
        }

        if (usuario.Rol == rol)
        {
            return Hecho($"{usuario.Correo} ya tiene el rol {rol}");
        }

        var cambio = await directorio.CambiarRolAsync(id, rol, cancelacion);
        return Resultado(cambio, $"Rol cambiado: {usuario.Correo} ahora es {rol}");
    }

    /// <summary>
    /// Pone una contraseña nueva a un usuario que olvidó la suya.
    /// </summary>
    /// <param name="id">Usuario.</param>
    /// <param name="contrasena">Contraseña nueva, que debe cumplir la política.</param>
    /// <param name="cancelacion">Permite cancelar la operación.</param>
    /// <returns>Si se cambió y el texto que se muestra.</returns>
    public async Task<ResultadoDeAdministracion> RestablecerContrasenaAsync(
        Guid id,
        string? contrasena,
        CancellationToken cancelacion = default)
    {
        if (string.IsNullOrWhiteSpace(contrasena))
        {
            return Rechazar(FaltaLaContrasena);
        }

        var usuario = await directorio.ObtenerAsync(id, cancelacion);

        if (usuario is null)
        {
            return Rechazar(UsuarioNoEncontrado);
        }

        var cambio = await directorio.RestablecerContrasenaAsync(id, contrasena, cancelacion);
        return Resultado(cambio, $"Contraseña restablecida: {usuario.Correo}");
    }

    /// <summary>
    /// Desactiva un usuario: no puede iniciar sesión, y sus sesiones abiertas se cierran.
    /// </summary>
    /// <param name="id">Usuario que se desactiva.</param>
    /// <param name="correoDeLaSesion">Correo del administrador que lo desactiva.</param>
    /// <param name="cancelacion">Permite cancelar la operación.</param>
    /// <returns>Si se desactivó y el texto que se muestra.</returns>
    public async Task<ResultadoDeAdministracion> DesactivarAsync(
        Guid id,
        string correoDeLaSesion,
        CancellationToken cancelacion = default)
    {
        var usuario = await directorio.ObtenerAsync(id, cancelacion);

        if (usuario is null)
        {
            return Rechazar(UsuarioNoEncontrado);
        }

        if (EsElDeLaSesion(usuario, correoDeLaSesion))
        {
            return Rechazar(NoPuedeDesactivarse);
        }

        if (EstaDesactivado(usuario.BloqueadoHasta))
        {
            return Rechazar(YaEstaDesactivado);
        }

        var cambio = await directorio.DesactivarAsync(id, cancelacion);
        return Resultado(cambio, $"Usuario desactivado: {usuario.Correo}. Ya no puede iniciar sesión");
    }

    /// <summary>
    /// Reactiva un usuario desactivado.
    /// </summary>
    /// <param name="id">Usuario que se reactiva.</param>
    /// <param name="cancelacion">Permite cancelar la operación.</param>
    /// <returns>Si se reactivó y el texto que se muestra.</returns>
    public async Task<ResultadoDeAdministracion> ReactivarAsync(Guid id, CancellationToken cancelacion = default)
    {
        var usuario = await directorio.ObtenerAsync(id, cancelacion);

        if (usuario is null)
        {
            return Rechazar(UsuarioNoEncontrado);
        }

        if (!EstaDesactivado(usuario.BloqueadoHasta))
        {
            return Rechazar(NoEstaDesactivado);
        }

        var cambio = await directorio.QuitarBloqueoAsync(id, cancelacion);
        return Resultado(cambio, $"Usuario reactivado: {usuario.Correo}");
    }

    /// <summary>
    /// Desbloquea un usuario bloqueado por intentos fallidos, sin esperar a que pase el tiempo del bloqueo.
    /// </summary>
    /// <param name="id">Usuario que se desbloquea.</param>
    /// <param name="cancelacion">Permite cancelar la operación.</param>
    /// <returns>Si se desbloqueó y el texto que se muestra.</returns>
    public async Task<ResultadoDeAdministracion> DesbloquearAsync(Guid id, CancellationToken cancelacion = default)
    {
        var usuario = await directorio.ObtenerAsync(id, cancelacion);

        if (usuario is null)
        {
            return Rechazar(UsuarioNoEncontrado);
        }

        // Un usuario desactivado no se desbloquea: se reactiva, para que la acción diga lo que hace.
        if (EstadoSegunElBloqueo(usuario.BloqueadoHasta, reloj.GetUtcNow()) != EstadoDeUsuario.Bloqueado)
        {
            return Rechazar(NoEstaBloqueado);
        }

        var cambio = await directorio.QuitarBloqueoAsync(id, cancelacion);
        return Resultado(cambio, $"Usuario desbloqueado: {usuario.Correo}");
    }

    /// <summary>
    /// Borra un usuario que nunca registró un pago. Si registró pagos, se rechaza y se ofrece desactivarlo.
    /// </summary>
    /// <param name="id">Usuario que se borra.</param>
    /// <param name="correoDeLaSesion">Correo del administrador que lo borra.</param>
    /// <param name="cancelacion">Permite cancelar la operación.</param>
    /// <returns>Si se borró y el texto que se muestra.</returns>
    public async Task<ResultadoDeAdministracion> BorrarAsync(
        Guid id,
        string correoDeLaSesion,
        CancellationToken cancelacion = default)
    {
        var usuario = await directorio.ObtenerAsync(id, cancelacion);

        if (usuario is null)
        {
            return Rechazar(UsuarioNoEncontrado);
        }

        if (EsElDeLaSesion(usuario, correoDeLaSesion))
        {
            return Rechazar(NoPuedeBorrarse);
        }

        // Cada pago guarda el correo de quien lo registró: borrar al usuario dejaría pagos de alguien que no existe.
        if (await pagos.ExistenDelOperadorAsync(usuario.Correo, cancelacion))
        {
            return Rechazar(TienePagos);
        }

        var cambio = await directorio.BorrarAsync(id, cancelacion);
        return Resultado(cambio, $"Usuario borrado: {usuario.Correo}");
    }

    // Quien actúa es un Administrador activo. Si no puede actuar sobre sí mismo, siempre queda al menos uno.
    private static bool EsElDeLaSesion(UsuarioDelDirectorio usuario, string correoDeLaSesion)
    {
        return string.Equals(usuario.Correo, correoDeLaSesion, StringComparison.OrdinalIgnoreCase);
    }

    private static ResultadoDeAdministracion Resultado(ResultadoDeCambioDeUsuario cambio, string textoSiSeHizo)
    {
        return cambio.Hecho ? Hecho(textoSiSeHizo) : Rechazar(cambio.Motivo);
    }

    private static ResultadoDeAdministracion Hecho(string motivo)
    {
        return new ResultadoDeAdministracion(true, motivo);
    }

    private static ResultadoDeAdministracion Rechazar(string motivo)
    {
        return new ResultadoDeAdministracion(false, motivo);
    }
}
