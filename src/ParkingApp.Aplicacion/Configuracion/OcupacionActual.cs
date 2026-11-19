namespace ParkingApp.Aplicacion.Configuracion;

/// <summary>Vehículos que tienen un ingreso como último evento registrado.</summary>
/// <param name="Carros">Cantidad de carros dentro de la sede.</param>
/// <param name="Motos">Cantidad de motos dentro de la sede.</param>
public sealed record OcupacionActual(int Carros, int Motos);
