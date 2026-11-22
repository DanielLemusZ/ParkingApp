using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

using ParkingApp.Aplicacion.Accesos;
using ParkingApp.Aplicacion.Accesos.Validaciones;
using ParkingApp.Aplicacion.Cobros;
using ParkingApp.Aplicacion.Configuracion;
using ParkingApp.Aplicacion.Identidad;
using ParkingApp.Aplicacion.Pagos;
using ParkingApp.Aplicacion.Reportes;
using ParkingApp.Aplicacion.Tarifas;

namespace ParkingApp.Aplicacion;

/// <summary>
/// Registro de los casos de uso de la aplicación.
/// </summary>
public static class ServiciosDeAplicacion
{
    /// <summary>
    /// Registra los casos de uso, la cadena de validaciones del carril y el reloj del sistema.
    /// </summary>
    /// <param name="servicios">Contenedor de servicios de la aplicación.</param>
    /// <returns>El mismo contenedor de servicios.</returns>
    public static IServiceCollection AgregarAplicacion(this IServiceCollection servicios)
    {
        servicios.TryAddSingleton(TimeProvider.System);

        // Por alcance, porque el horario de la sede se lee de la base de datos.
        servicios.AddScoped<CalendarioOperativo>();

        // La cadena se arma de nuevo en cada alcance, porque cada eslabón guarda cuál es el siguiente.
        servicios.AddScoped(CrearCadenaDeIngreso);

        servicios.AddScoped<RegistrarIngreso>();
        servicios.AddScoped<RegistrarSalida>();
        servicios.AddScoped<AdministrarTarifas>();
        servicios.AddScoped<IServicioConsultas, ConsultarEventos>();
        servicios.AddScoped<ConfigurarSede>();
        servicios.AddScoped<CrearUsuariosIniciales>();
        servicios.AddScoped<AdministrarUsuarios>();
        servicios.AddScoped<ListarCobrosPendientes>();
        servicios.AddScoped<RegistrarPagoEnEfectivo>();

        return servicios;
    }

    /// <summary>
    /// Arma la cadena de validaciones del carril de entrada.
    /// Orden: lectura válida → clasificación por formato → umbral de confianza → cupo → cobro o pase vigente.
    /// Cada historia agrega su eslabón en su lugar de la lista.
    /// </summary>
    /// <param name="proveedor">Proveedor de servicios, para los eslabones que necesiten consultar algo.</param>
    /// <returns>El primer eslabón de la cadena.</returns>
    private static ValidacionDeIngreso CrearCadenaDeIngreso(IServiceProvider proveedor)
    {
        return CadenaDeValidacionesDeIngreso.Crear(
            new LecturaValida(),
            new ClasificacionDeVehiculoValida(),
            new CupoDisponible(proveedor.GetRequiredService<IRepositorioSedes>()),
            new TarifaVigente(
                proveedor.GetRequiredService<IRepositorioTarifas>(),
                proveedor.GetRequiredService<CalendarioOperativo>()));
    }
}
