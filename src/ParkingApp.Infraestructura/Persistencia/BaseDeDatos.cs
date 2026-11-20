using System.Globalization;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Npgsql;

using ParkingApp.Aplicacion.Accesos;
using ParkingApp.Aplicacion.Cobros;
using ParkingApp.Aplicacion.Configuracion;
using ParkingApp.Aplicacion.Tarifas;
using ParkingApp.Infraestructura.Configuracion;
using ParkingApp.Infraestructura.Persistencia.Repositorios;

namespace ParkingApp.Infraestructura.Persistencia;

/// <summary>
/// Registro de la base de datos PostgreSQL y verificación de la conexión al arrancar.
/// </summary>
public static class BaseDeDatos
{
    private const string ComandoParaActualizar =
        "dotnet ef database update --project src/ParkingApp.Infraestructura --startup-project src/ParkingApp.Web";

    /// <summary>
    /// Registra el contexto de EF Core, los repositorios y la sede del nodo.
    /// La cadena de conexión se arma con las variables <c>POSTGRES_*</c> de <c>.env</c>.
    /// </summary>
    /// <param name="servicios">Contenedor de servicios de la aplicación.</param>
    /// <param name="configuracion">Configuración, con las variables de entorno ya cargadas.</param>
    /// <param name="esDesarrollo">Si es el entorno de desarrollo, se muestran errores detallados de EF Core.</param>
    /// <returns>El mismo contenedor de servicios.</returns>
    /// <exception cref="InvalidOperationException">Falta una variable obligatoria en <c>.env</c>.</exception>
    public static IServiceCollection AgregarBaseDeDatos(
        this IServiceCollection servicios,
        IConfiguration configuracion,
        bool esDesarrollo)
    {
        var cadenaDeConexion = CrearCadenaDeConexion(configuracion).ConnectionString;

        servicios.AddDbContext<ParkingAppDbContext>(opciones =>
        {
            opciones.UseNpgsql(cadenaDeConexion);
            opciones.UseSnakeCaseNamingConvention();

            if (esDesarrollo)
            {
                opciones.EnableDetailedErrors();
            }
        });

        servicios.AddScoped<IRepositorioEventosDeAcceso, RepositorioEventosDeAcceso>();
        servicios.AddScoped<ISalidaRepository, SalidaRepository>();
        servicios.AddScoped<IRepositorioTarifas, RepositorioTarifas>();
        servicios.AddScoped<IRepositorioCobros, RepositorioCobros>();
        servicios.AddScoped<IRepositorioSedes, RepositorioSedes>();
        servicios.AddSingleton<ISedeActual, SedeDelNodo>();
        servicios.AddScoped<IHorarioDeOperacionActual, HorarioDeOperacionDeLaSede>();

        return servicios;
    }

    /// <summary>
    /// Comprueba que PostgreSQL responda y avisa si hay migraciones sin aplicar.
    /// Si no responde, registra el motivo sin mostrar la contraseña.
    /// </summary>
    /// <param name="servicios">Proveedor de servicios de la aplicación ya construida.</param>
    /// <param name="registro">Registro donde se escribe el resultado.</param>
    /// <returns><c>true</c> si la conexión funciona; <c>false</c> si no.</returns>
    public static async Task<bool> VerificarConexionAsync(this IServiceProvider servicios, ILogger registro)
    {
        await using var alcance = servicios.CreateAsyncScope();
        var contexto = alcance.ServiceProvider.GetRequiredService<ParkingAppDbContext>();
        var destino = new NpgsqlConnectionStringBuilder(contexto.Database.GetConnectionString());

        try
        {
            // Abrir la conexión primero distingue una base inexistente de una base sin migraciones.
            await contexto.Database.OpenConnectionAsync();
            var migracionesPendientes = await contexto.Database.GetPendingMigrationsAsync();
            var pendientes = migracionesPendientes.ToList();
            await contexto.Database.CloseConnectionAsync();

            if (pendientes.Count > 0)
            {
                registro.LogWarning(
                    "Hay migraciones sin aplicar en la base de datos ({Cantidad}): {Migraciones}. Ejecuta: {Comando}",
                    pendientes.Count,
                    string.Join(", ", pendientes),
                    ComandoParaActualizar);
            }

            return true;
        }
        catch (Exception error)
        {
            var errorDeNpgsql = BuscarErrorDeNpgsql(error);

            if (errorDeNpgsql == null)
            {
                // No es un problema de conexión con PostgreSQL: se deja pasar el error original.
                throw;
            }

            ExplicarFallaDeConexion(errorDeNpgsql, destino, registro);
            return false;
        }
    }

    /// <summary>
    /// Arma la cadena de conexión con <c>POSTGRES_HOST</c> (por defecto <c>localhost</c>), <c>POSTGRES_PORT</c>,
    /// <c>POSTGRES_DB</c>, <c>POSTGRES_USER</c> y <c>POSTGRES_PASSWORD</c>.
    /// </summary>
    internal static NpgsqlConnectionStringBuilder CrearCadenaDeConexion(IConfiguration configuracion)
    {
        var host = configuracion["POSTGRES_HOST"];

        if (string.IsNullOrEmpty(host))
        {
            host = "localhost";
        }

        var puerto = LeerObligatoria(configuracion, "POSTGRES_PORT");

        if (!int.TryParse(puerto, NumberStyles.None, CultureInfo.InvariantCulture, out var numeroDePuerto))
        {
            throw new InvalidOperationException($"POSTGRES_PORT debe ser un número de puerto, y en .env dice '{puerto}'.");
        }

        var cadena = new NpgsqlConnectionStringBuilder();
        cadena.Host = host;
        cadena.Port = numeroDePuerto;
        cadena.Database = LeerObligatoria(configuracion, "POSTGRES_DB");
        cadena.Username = LeerObligatoria(configuracion, "POSTGRES_USER");
        cadena.Password = LeerObligatoria(configuracion, "POSTGRES_PASSWORD");

        return cadena;
    }

    private static string LeerObligatoria(IConfiguration configuracion, string variable)
    {
        var valor = configuracion[variable];

        if (string.IsNullOrEmpty(valor))
        {
            throw new InvalidOperationException($"Falta la variable {variable} en .env (copia .env.example como .env).");
        }

        return valor;
    }

    // Npgsql envuelve los fallos de red (por ejemplo, la base apagada) en una InvalidOperationException
    // de "falla transitoria"; el error real queda adentro.
    private static NpgsqlException? BuscarErrorDeNpgsql(Exception error)
    {
        if (error is NpgsqlException errorDirecto)
        {
            return errorDirecto;
        }

        if (error.InnerException is NpgsqlException errorInterno)
        {
            return errorInterno;
        }

        return null;
    }

    private static void ExplicarFallaDeConexion(NpgsqlException error, NpgsqlConnectionStringBuilder destino, ILogger registro)
    {
        if (!registro.IsEnabled(LogLevel.Critical))
        {
            return;
        }

        // Los errores que responde PostgreSQL traen un código (SqlState). Los de red, como la base apagada, no.
        var codigo = string.Empty;

        if (error is PostgresException errorDePostgres)
        {
            codigo = errorDePostgres.SqlState;
        }

        if (codigo == PostgresErrorCodes.InvalidPassword)
        {
            registro.LogCritical(
                "No se pudo conectar a PostgreSQL en {Host}:{Puerto} (base {Base}, usuario {Usuario}): la contraseña no es correcta. Revisa POSTGRES_PASSWORD en .env.",
                destino.Host,
                destino.Port,
                destino.Database,
                destino.Username);
        }
        else if (codigo == PostgresErrorCodes.InvalidCatalogName)
        {
            registro.LogCritical(
                "PostgreSQL responde en {Host}:{Puerto}, pero la base {Base} no existe. Revisa POSTGRES_DB en .env, o créala con: {Comando}",
                destino.Host,
                destino.Port,
                destino.Database,
                ComandoParaActualizar);
        }
        else
        {
            registro.LogCritical(
                "PostgreSQL no está disponible en {Host}:{Puerto}. ¿Levantaste el contenedor? Ejecuta: docker compose up -d",
                destino.Host,
                destino.Port);
        }
    }
}
