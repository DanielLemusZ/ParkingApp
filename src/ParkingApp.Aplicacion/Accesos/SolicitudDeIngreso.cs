namespace ParkingApp.Aplicacion.Accesos;

/// <summary>
/// Lectura que llega al carril de entrada y que recorre la cadena de validaciones.
/// </summary>
/// <param name="Placa">Texto de la placa, ya normalizado. Puede estar vacío.</param>
/// <param name="Confianza">Confianza de la lectura, que debería estar entre 0 y 100.</param>
/// <param name="Carril">Número del carril de entrada.</param>
public sealed record SolicitudDeIngreso(string Placa, int Confianza, int Carril);
