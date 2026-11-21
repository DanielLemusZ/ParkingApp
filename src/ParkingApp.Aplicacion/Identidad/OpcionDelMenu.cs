namespace ParkingApp.Aplicacion.Identidad;

/// <summary>
/// Opción del menú de la aplicación y los roles que la pueden ver.
/// </summary>
/// <param name="Texto">Texto que se muestra en el menú.</param>
/// <param name="Ruta">Dirección de la pantalla, por ejemplo <c>/caja</c>.</param>
/// <param name="RolesPermitidos">Roles que pueden abrir la pantalla.</param>
public sealed record OpcionDelMenu(string Texto, string Ruta, IReadOnlyList<string> RolesPermitidos);
