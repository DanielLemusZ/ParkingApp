using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using ParkingApp.Aplicacion.Accesos;
using ParkingApp.Aplicacion.Pagos;
using ParkingApp.Infraestructura.LecturaDePlacas;
using ParkingApp.Infraestructura.ServiciosExternos.Pasarela;

namespace ParkingApp.Infraestructura.ServiciosExternos;

/// <summary>
/// Registro de los adaptadores de servicios externos. La facturación es simulada y el lector de placas corre en el
/// mismo equipo. La pasarela de pagos es la simulada, o el sandbox de PayU si así se configura en <c>.env</c>.
/// </summary>
public static class ServiciosExternos
{
    /// <summary>
    /// Registra el adaptador simulado de facturación electrónica, el lector de placas y la pasarela de pagos.
    /// </summary>
    /// <param name="servicios">Contenedor de servicios de la aplicación.</param>
    /// <param name="configuracion">Configuración, con las variables de entorno ya cargadas.</param>
    /// <returns>El mismo contenedor de servicios.</returns>
    /// <exception cref="InvalidOperationException">La configuración de la pasarela no es válida.</exception>
    public static IServiceCollection AgregarServiciosExternos(this IServiceCollection servicios, IConfiguration configuracion)
    {
        servicios.AddScoped<IFacturacionElectronica, AdaptadorFacturacionSimulado>();

        // Uno solo para toda la aplicación: carga los modelos una vez y atiende varias lecturas a la vez.
        servicios.AddSingleton<ILectorDePlacas, LectorDePlacasOnnx>();

        AgregarPasarela(servicios, OpcionesDePasarela.Leer(configuracion));
        return servicios;
    }

    // Todo es singleton: la simulada guarda sus transacciones en memoria y el respaldo cuenta las fallas de toda la sede.
    private static void AgregarPasarela(IServiceCollection servicios, OpcionesDePasarela opciones)
    {
        servicios.AddSingleton(opciones);
        servicios.AddSingleton(opciones.PayU);
        servicios.AddSingleton<AdaptadorPasarelaSimulada>();

        // Los dos adaptadores se registran siempre, porque sus pantallas (la pasarela simulada y el formulario de PayU)
        // existen siempre; solo el proveedor elegido atiende la caja.
        servicios.AddHttpClient(AdaptadorPayU.NombreDelCliente, cliente => cliente.Timeout = TimeSpan.FromSeconds(15));
        servicios.AddSingleton<AdaptadorPayU>();

        if (opciones.Proveedor == ProveedorDePasarela.Simulada)
        {
            servicios.AddSingleton<IPasarelaDePagos>(proveedor => proveedor.GetRequiredService<AdaptadorPasarelaSimulada>());
            return;
        }

        servicios.AddSingleton<IPasarelaDePagos>(proveedor => new PasarelaConRespaldo(
            proveedor.GetRequiredService<AdaptadorPayU>(),
            proveedor.GetRequiredService<AdaptadorPasarelaSimulada>(),
            opciones,
            proveedor.GetRequiredService<TimeProvider>(),
            proveedor.GetRequiredService<ILogger<PasarelaConRespaldo>>()));
    }
}
