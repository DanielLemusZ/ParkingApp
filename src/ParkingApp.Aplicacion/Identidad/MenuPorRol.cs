namespace ParkingApp.Aplicacion.Identidad;

/// <summary>
/// Decide qué pestañas del menú ve cada rol (F-10): el Administrador ve todo; el Operador, solo el carril
/// (<em>Monitoreo de acceso</em>) y la caja. Las pantallas y la API aplican la misma regla con
/// <c>[Authorize(Roles = ...)]</c>; el menú solo evita mostrar opciones que el usuario no puede abrir.
/// Tarifas, franjas, cupos, umbral y usuarios viven en el submenú de <em>Configuración</em>, y los ingresos, salidas
/// y cobros en el de <em>Historial</em>, como en los mockups (#71).
/// </summary>
public static class MenuPorRol
{
    private static readonly IReadOnlyList<string> OperadorOAdministrador = [Roles.Operador, Roles.Administrador];
    private static readonly IReadOnlyList<string> SoloAdministrador = [Roles.Administrador];

    /// <summary>
    /// Todas las pestañas del menú, en el orden en que se muestran.
    /// </summary>
    public static IReadOnlyList<OpcionDelMenu> Opciones { get; } =
    [
        new OpcionDelMenu("Monitoreo de acceso", "/carril", OperadorOAdministrador),
        new OpcionDelMenu("Caja", "/caja", OperadorOAdministrador),
        new OpcionDelMenu("Historial", "/historial", SoloAdministrador),
        new OpcionDelMenu("Configuración", "/configuracion", SoloAdministrador),
    ];

    /// <summary>
    /// Opciones del menú que puede ver un usuario con los roles dados.
    /// </summary>
    /// <param name="roles">Roles del usuario que inició sesión.</param>
    /// <returns>Las opciones permitidas, en el orden del menú; ninguna si el usuario no tiene un rol conocido.</returns>
    public static IReadOnlyList<OpcionDelMenu> Para(IEnumerable<string> roles)
    {
        ArgumentNullException.ThrowIfNull(roles);

        var delUsuario = roles.ToHashSet(StringComparer.Ordinal);

        return Opciones
            .Where(opcion => opcion.RolesPermitidos.Any(delUsuario.Contains))
            .ToList();
    }
}
