using System.Globalization;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Npgsql;

namespace ParkingApp.Infraestructura.Persistencia;

/// <summary>
/// Registro de la base de datos PostgreSQL y verificación de la conexión al arrancar.
/// </summary>
public static partial class BaseDeDatos
{
    /// <summary>
    /// Registra el contexto de EF Core. La cadena de conexión se arma con las variables <c>POSTGRES_*</c> de <c>.env</c>.
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
            opciones.UseNpgsql(cadenaDeConexion).UseSnakeCaseNamingConvention();

            if (esDesarrollo)
            {
                opciones.EnableDetailedErrors();
            }
        });

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
            var pendientes = (await contexto.Database.GetPendingMigrationsAsync()).ToList();
            await contexto.Database.CloseConnectionAsync();

            if (pendientes.Count > 0)
            {
                MigracionesPendientes(registro, pendientes.Count, string.Join(", ", pendientes));
            }

            return true;
        }
        catch (Exception error) when (ErrorDeNpgsql(error) is { } errorDeNpgsql)
        {
            switch (errorDeNpgsql)
            {
                case PostgresException { SqlState: PostgresErrorCodes.InvalidPassword }:
                    ContrasenaIncorrecta(registro, destino.Host, destino.Port, destino.Database, destino.Username);
                    break;
                case PostgresException { SqlState: PostgresErrorCodes.InvalidCatalogName }:
                    BaseInexistente(registro, destino.Host, destino.Port, destino.Database);
                    break;
                default:
                    BaseNoDisponible(registro, destino.Host, destino.Port);
                    break;
            }
        }

        return false;
    }

    // Npgsql envuelve los fallos de red (por ejemplo, la base apagada) en una InvalidOperationException
    // de "falla transitoria"; el error real queda adentro.
    private static NpgsqlException? ErrorDeNpgsql(Exception error) =>
        error as NpgsqlException ?? error.InnerException as NpgsqlException;

    /// <summary>
    /// Arma la cadena de conexión con <c>POSTGRES_HOST</c> (por defecto <c>localhost</c>), <c>POSTGRES_PORT</c>,
    /// <c>POSTGRES_DB</c>, <c>POSTGRES_USER</c> y <c>POSTGRES_PASSWORD</c>.
    /// </summary>
    internal static NpgsqlConnectionStringBuilder CrearCadenaDeConexion(IConfiguration configuracion)
    {
        var puerto = Obligatoria(configuracion, "POSTGRES_PORT");

        if (!int.TryParse(puerto, NumberStyles.None, CultureInfo.InvariantCulture, out var numeroDePuerto))
        {
            throw new InvalidOperationException($"POSTGRES_PORT debe ser un número de puerto, y en .env dice '{puerto}'.");
        }

        return new NpgsqlConnectionStringBuilder
        {
            Host = configuracion["POSTGRES_HOST"] is { Length: > 0 } host ? host : "localhost",
            Port = numeroDePuerto,
            Database = Obligatoria(configuracion, "POSTGRES_DB"),
            Username = Obligatoria(configuracion, "POSTGRES_USER"),
            Password = Obligatoria(configuracion, "POSTGRES_PASSWORD"),
        };
    }

    private static string Obligatoria(IConfiguration configuracion, string variable) =>
        configuracion[variable] is { Length: > 0 } valor
            ? valor
            : throw new InvalidOperationException($"Falta la variable {variable} en .env (copia .env.example como .env).");

    [LoggerMessage(Level = LogLevel.Critical, Message =
        "No se pudo conectar a PostgreSQL en {Host}:{Puerto} (base {Base}, usuario {Usuario}): la contraseña no es correcta. Revisa POSTGRES_PASSWORD en .env.")]
    private static partial void ContrasenaIncorrecta(ILogger registro, string? host, int puerto, string? @base, string? usuario);

    [LoggerMessage(Level = LogLevel.Critical, Message =
        "PostgreSQL responde en {Host}:{Puerto}, pero la base {Base} no existe. Revisa POSTGRES_DB en .env, o créala con: dotnet ef database update --project src/ParkingApp.Infraestructura --startup-project src/ParkingApp.Web")]
    private static partial void BaseInexistente(ILogger registro, string? host, int puerto, string? @base);

    [LoggerMessage(Level = LogLevel.Critical, Message =
        "PostgreSQL no está disponible en {Host}:{Puerto}. ¿Levantaste el contenedor? Ejecuta: docker compose up -d")]
    private static partial void BaseNoDisponible(ILogger registro, string? host, int puerto);

    [LoggerMessage(Level = LogLevel.Warning, Message =
        "Hay migraciones sin aplicar en la base de datos ({Cantidad}): {Migraciones}. Ejecuta: dotnet ef database update --project src/ParkingApp.Infraestructura --startup-project src/ParkingApp.Web")]
    private static partial void MigracionesPendientes(ILogger registro, int cantidad, string migraciones);
}
