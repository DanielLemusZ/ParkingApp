using ParkingApp.Dominio.Configuracion;

namespace ParkingApp.Aplicacion.Tarifas;

/// <summary>
/// Resultado de solicitar un cambio de tarifa.
/// </summary>
/// <param name="Guardado">Si se creó la nueva versión.</param>
/// <param name="Motivo">Explicación si se rechazó el cambio.</param>
/// <param name="Tarifa">La nueva versión guardada, si se aceptó.</param>
public sealed record ResultadoDeCambioDeTarifa(bool Guardado, string Motivo, Tarifa? Tarifa);
