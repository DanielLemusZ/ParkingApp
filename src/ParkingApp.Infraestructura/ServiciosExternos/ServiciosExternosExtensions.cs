using Microsoft.Extensions.DependencyInjection;
using ParkingApp.Aplicacion.Pagos;

namespace ParkingApp.Infraestructura.ServiciosExternos;

public static class ServiciosExternosExtensions
{
    public static IServiceCollection AgregarServiciosExternos(this IServiceCollection services)
    {
        services.AddScoped<IFacturacionElectronica, AdaptadorFacturacionSimulado>();
        return services;
    }
}