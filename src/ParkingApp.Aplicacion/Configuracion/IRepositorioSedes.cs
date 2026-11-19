using ParkingApp.Dominio.Configuracion;

namespace ParkingApp.Aplicacion.Configuracion;

/// <summary>Consulta y guarda la configuración de la sede, y consulta su ocupación actual.</summary>
public interface IRepositorioSedes
{
    /// <summary>Obtiene la sede que usa esta instalación.</summary>
    Task<Sede> ObtenerAsync(CancellationToken cancelacion = default);

    /// <summary>Guarda los cambios de configuración de la sede.</summary>
    Task GuardarCambiosAsync(Sede sede, CancellationToken cancelacion = default);

    /// <summary>Cuenta los vehículos cuyo evento más reciente es un ingreso.</summary>
    Task<OcupacionActual> ConsultarOcupacionAsync(CancellationToken cancelacion = default);
}
