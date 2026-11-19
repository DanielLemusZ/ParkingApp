namespace ParkingApp.Aplicacion.Configuracion;

/// <summary>Configuración de la sede junto con la ocupación por tipo de vehículo.</summary>
/// <param name="InicioFranjaDiurna">Hora de inicio de la franja diurna.</param>
/// <param name="InicioFranjaNocturna">Hora de inicio de la franja nocturna.</param>
/// <param name="CupoCarros">Cupo máximo de carros.</param>
/// <param name="CupoMotos">Cupo máximo de motos.</param>
/// <param name="UmbralConfianza">Confianza mínima aceptada.</param>
/// <param name="Ocupacion">Vehículos dentro de la sede.</param>
public sealed record DatosDeConfiguracionSede(
    TimeOnly InicioFranjaDiurna,
    TimeOnly InicioFranjaNocturna,
    int CupoCarros,
    int CupoMotos,
    int UmbralConfianza,
    OcupacionActual Ocupacion);
