namespace ParkingApp.Aplicacion.Configuracion;

/// <summary>
/// Sede donde corre este nodo.
/// </summary>
public interface ISedeActual
{
    /// <summary>
    /// Identificador de la sede.
    /// </summary>
    Guid Id { get; }

    /// <summary>
    /// Zona horaria de la sede. Las horas se guardan en UTC y se muestran en esta zona.
    /// </summary>
    TimeZoneInfo ZonaHoraria { get; }
}
