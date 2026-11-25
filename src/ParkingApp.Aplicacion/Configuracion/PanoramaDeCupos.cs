using ParkingApp.Aplicacion.Accesos;

namespace ParkingApp.Aplicacion.Configuracion;

/// <summary>
/// Lo que muestra la página <em>Cupos</em>: la ocupación de cada tipo y los vehículos que siguen adentro.
/// </summary>
/// <param name="Carros">Ocupación de los carros frente a su cupo.</param>
/// <param name="Motos">Ocupación de las motos frente a su cupo.</param>
/// <param name="Adentro">Vehículos adentro, del ingreso más antiguo al más reciente.</param>
public sealed record PanoramaDeCupos(EstadoDeCupo Carros, EstadoDeCupo Motos, IReadOnlyList<VehiculoAdentro> Adentro);
