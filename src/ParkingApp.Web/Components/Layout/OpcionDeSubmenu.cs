namespace ParkingApp.Web.Components.Layout;

/// <summary>
/// Opción del submenú lateral de Historial y Configuración (#71).
/// </summary>
/// <param name="Texto">Lo que se lee en el menú.</param>
/// <param name="Ruta">Dirección de la subpágina.</param>
/// <param name="Icono">Ícono de MudBlazor.</param>
public sealed record OpcionDeSubmenu(string Texto, string Ruta, string Icono);
