using Microsoft.Extensions.DependencyInjection;

using ParkingApp.Aplicacion.Pagos;

namespace ParkingApp.Infraestructura.ServiciosExternos;

public static class ServiciosExternos
{
    public static IServiceCollection AgregarServiciosExternos(this IServiceCollection services)
    {
        services.AddScoped<IFacturacionElectronica, AdaptadorFacturacionSimulado>();
        return services;
    }
}
