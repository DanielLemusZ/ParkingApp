namespace ParkingApp.Aplicacion.Identidad;

/// <summary>
/// Roles de los usuarios (F-10). Los nombres están en un solo lugar para que la semilla de la base de datos,
/// las pantallas y la API usen exactamente los mismos.
/// </summary>
public static class Roles
{
    /// <summary>
    /// Opera el carril y la caja.
    /// </summary>
    public const string Operador = "Operador";

    /// <summary>
    /// Accede a todo: carril, caja, configuración y consultas.
    /// </summary>
    public const string Administrador = "Administrador";

    /// <summary>
    /// Valor para <c>[Authorize(Roles = ...)]</c> en lo que pueden usar los dos roles: el carril y la caja.
    /// La coma significa "cualquiera de estos roles".
    /// </summary>
    public const string OperadorOAdministrador = Operador + "," + Administrador;

    /// <summary>
    /// Todos los roles que existen.
    /// </summary>
    public static IReadOnlyList<string> Todos { get; } = [Operador, Administrador];
}
