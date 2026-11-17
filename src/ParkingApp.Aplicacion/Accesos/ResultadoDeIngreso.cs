namespace ParkingApp.Aplicacion.Accesos;

/// <summary>
/// Respuesta del carril de entrada: si la barrera abre, por qué, y el ingreso registrado.
/// </summary>
/// <param name="AbreBarrera">Si la barrera se abre.</param>
/// <param name="Motivo">Motivo que se muestra en el carril.</param>
/// <param name="Ingreso">El ingreso registrado, o nada si la barrera no abre.</param>
public sealed record ResultadoDeIngreso(bool AbreBarrera, string Motivo, IngresoRegistrado? Ingreso);

/// <summary>
/// Datos de un ingreso registrado, para mostrarlos en el carril.
/// </summary>
/// <param name="Id">Identificador del evento.</param>
/// <param name="Placa">Placa normalizada.</param>
/// <param name="FechaHora">Fecha y hora del ingreso, en la hora de la sede.</param>
/// <param name="Carril">Número del carril.</param>
/// <param name="Confianza">Confianza de la lectura.</param>
/// <param name="TipoDeVehiculo">Tipo de vehículo identificado por la placa.</param>
public sealed record IngresoRegistrado(Guid Id, string Placa, DateTimeOffset FechaHora, int Carril, int Confianza, ParkingApp.Dominio.Accesos.TipoDeVehiculo TipoDeVehiculo);
