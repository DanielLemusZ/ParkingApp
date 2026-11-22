using ParkingApp.Dominio.Accesos;

namespace ParkingApp.Aplicacion.Accesos;

/// <summary>
/// Respuesta del carril de salida: si la barrera abre, por qué, y la salida registrada.
/// </summary>
/// <param name="AbreBarrera">Si la barrera se abre.</param>
/// <param name="Motivo">Motivo que se muestra en el carril.</param>
/// <param name="Salida">La salida registrada, o nada si la barrera no abre.</param>
/// <param name="PendienteDeValidacion">Si la lectura quedó en la caja para que el operador la valide.</param>
public sealed record ResultadoDeSalida(bool AbreBarrera, string Motivo, SalidaRegistrada? Salida, bool PendienteDeValidacion = false);

/// <summary>
/// Datos de una salida registrada, para mostrarlos en el carril.
/// </summary>
/// <param name="Id">Identificador del evento.</param>
/// <param name="Placa">Placa normalizada.</param>
/// <param name="FechaHora">Fecha y hora de la salida, en la hora de la sede.</param>
/// <param name="Carril">Número del carril.</param>
/// <param name="Confianza">Confianza de la lectura.</param>
/// <param name="TipoDeVehiculo">Tipo de vehículo identificado por la placa, o no clasificado.</param>
public sealed record SalidaRegistrada(Guid Id, string Placa, DateTimeOffset FechaHora, int Carril, int Confianza, TipoDeVehiculo TipoDeVehiculo);
