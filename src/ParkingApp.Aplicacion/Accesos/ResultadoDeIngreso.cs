using ParkingApp.Dominio.Accesos;
using ParkingApp.Dominio.Cobros;
using ParkingApp.Dominio.Configuracion;

namespace ParkingApp.Aplicacion.Accesos;

/// <summary>
/// Respuesta del carril de entrada: si la barrera abre, por qué, y el ingreso registrado.
/// </summary>
/// <param name="AbreBarrera">Si la barrera se abre.</param>
/// <param name="Motivo">Motivo que se muestra en el carril.</param>
/// <param name="Ingreso">El ingreso registrado, o nada si la barrera no abre.</param>
/// <param name="PendienteDeValidacion">Si la lectura quedó en la caja para que el operador la valide.</param>
/// <param name="LecturaPendiente">La lectura que quedó pendiente, para validarla también desde el carril.</param>
public sealed record ResultadoDeIngreso(
    bool AbreBarrera,
    string Motivo,
    IngresoRegistrado? Ingreso,
    bool PendienteDeValidacion = false,
    LecturaPorRevisar? LecturaPendiente = null);

/// <summary>
/// Datos de un ingreso registrado, para mostrarlos en el carril.
/// </summary>
/// <param name="Id">Identificador del evento.</param>
/// <param name="Placa">Placa normalizada.</param>
/// <param name="FechaHora">Fecha y hora del ingreso, en la hora de la sede.</param>
/// <param name="Carril">Número del carril.</param>
/// <param name="Confianza">Confianza de la lectura.</param>
/// <param name="TipoDeVehiculo">Tipo de vehículo identificado por la placa o indicado por el operador.</param>
/// <param name="Cobro">El cobro generado en este ingreso, o nada si no se generó.</param>
public sealed record IngresoRegistrado(Guid Id, string Placa, DateTimeOffset FechaHora, int Carril, int Confianza, TipoDeVehiculo TipoDeVehiculo, CobroGenerado? Cobro);

/// <summary>
/// Datos del cobro generado al ingresar, para mostrarlos en el carril.
/// </summary>
/// <param name="Id">Identificador del cobro.</param>
/// <param name="Valor">Valor en pesos colombianos.</param>
/// <param name="Franja">Franja del primer ingreso del día.</param>
/// <param name="DiaOperativo">Día operativo que cubre el cobro.</param>
/// <param name="Estado">Estado del cobro.</param>
public sealed record CobroGenerado(Guid Id, decimal Valor, FranjaTarifaria Franja, DateOnly DiaOperativo, EstadoDeCobro Estado);
