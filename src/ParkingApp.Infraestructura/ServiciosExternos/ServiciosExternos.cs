using Microsoft.Extensions.DependencyInjection;

using ParkingApp.Aplicacion.Accesos;
using ParkingApp.Aplicacion.Pagos;
using ParkingApp.Infraestructura.LecturaDePlacas;

namespace ParkingApp.Infraestructura.ServiciosExternos;

/// <summary>
/// Registro de los adaptadores de servicios externos. Ninguno llama a un servicio en internet: la facturación es simulada
/// y el lector de placas corre en el mismo equipo.
/// </summary>
public static class ServiciosExternos
{
    /// <summary>
    /// Registra el adaptador simulado de facturación electrónica y el lector de placas.
    /// </summary>
    /// <param name="servicios">Contenedor de servicios de la aplicación.</param>
    /// <returns>El mismo contenedor de servicios.</returns>
    public static IServiceCollection AgregarServiciosExternos(this IServiceCollection servicios)
    {
        servicios.AddScoped<IFacturacionElectronica, AdaptadorFacturacionSimulado>();

        // Uno solo para toda la aplicación: carga los modelos una vez y atiende varias lecturas a la vez.
        servicios.AddSingleton<ILectorDePlacas, LectorDePlacasOnnx>();
        return servicios;
    }
}
