using ParkingApp.Dominio.Accesos;

namespace ParkingApp.Aplicacion.Accesos;

/// <summary>
/// Lo que aporta el operador cuando valida una lectura de entrada en la caja (F-06).
/// </summary>
/// <param name="Operador">Operador que validó la placa.</param>
/// <param name="TipoIndicado">Carro o moto, que solo se usa si el formato de la placa no se reconoce.</param>
public sealed record ValidacionDelOperador(string Operador, TipoDeVehiculo? TipoIndicado);
