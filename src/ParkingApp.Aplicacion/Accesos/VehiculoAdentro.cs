using ParkingApp.Dominio.Accesos;

namespace ParkingApp.Aplicacion.Accesos;

/// <summary>
/// Vehículo que sigue adentro de la sede: su último evento es un ingreso.
/// </summary>
/// <param name="Placa">Placa del vehículo.</param>
/// <param name="TipoDeVehiculo">Tipo con el que entró.</param>
/// <param name="FechaHoraDeIngreso">Fecha y hora del último ingreso, en UTC.</param>
public sealed record VehiculoAdentro(Placa Placa, TipoDeVehiculo TipoDeVehiculo, DateTimeOffset FechaHoraDeIngreso);
