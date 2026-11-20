using Microsoft.Extensions.DependencyInjection;

using ParkingApp.Aplicacion.Pagos;

namespace ParkingApp.Infraestructura.ServiciosExternos;

/// <summary>
/// Registro de los adaptadores de servicios externos. En el MVP son simulados y no llaman a ningún servicio real.
/// </summary>
public static class ServiciosExternos
{
    /// <summary>
    /// Registra el adaptador simulado de facturación electrónica.
    /// </summary>
    /// <param name="servicios">Contenedor de servicios de la aplicación.</param>
    /// <returns>El mismo contenedor de servicios.</returns>
    public static IServiceCollection AgregarServiciosExternos(this IServiceCollection servicios)
    {
        servicios.AddScoped<IFacturacionElectronica, AdaptadorFacturacionSimulado>();
        return servicios;
    }
}
