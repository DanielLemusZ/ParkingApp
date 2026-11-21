using Microsoft.EntityFrameworkCore;

using ParkingApp.Aplicacion.Identidad;
using ParkingApp.Infraestructura.Persistencia;

namespace ParkingApp.Web.Cuenta;

/// <summary>
/// Crea los usuarios iniciales al arrancar, con las credenciales de <c>.env</c> (criterio 1 del issue #23).
/// </summary>
public static class UsuariosInicialesAlArrancar
{
    /// <summary>
    /// Lee <c>ADMIN_CORREO</c>, <c>ADMIN_CONTRASENA</c>, <c>OPERADOR_CORREO</c> y <c>OPERADOR_CONTRASENA</c>
    /// y crea los usuarios que falten. Deja en el registro qué pasó con cada uno, sin mostrar contraseñas.
    /// </summary>
    /// <param name="servicios">Proveedor de servicios de la aplicación ya construida.</param>
    /// <param name="configuracion">Configuración, con las variables de <c>.env</c> ya cargadas.</param>
    /// <param name="registro">Registro donde se escribe el resultado.</param>
    /// <returns>Una tarea que termina cuando se revisaron todos los usuarios.</returns>
    public static async Task CrearUsuariosInicialesAsync(this IServiceProvider servicios, IConfiguration configuracion, ILogger registro)
    {
        await using var alcance = servicios.CreateAsyncScope();

        // Sin las migraciones, las tablas de usuarios no existen todavía. VerificarConexionAsync ya avisó cuáles faltan.
        var contexto = alcance.ServiceProvider.GetRequiredService<ParkingAppDbContext>();
        var pendientes = await contexto.Database.GetPendingMigrationsAsync();

        if (pendientes.Any())
        {
            registro.LogWarning("No se crearon los usuarios iniciales porque hay migraciones sin aplicar.");
            return;
        }

        var usuarios = new[]
        {
            new UsuarioInicial(configuracion["ADMIN_CORREO"], configuracion["ADMIN_CONTRASENA"], Roles.Administrador),
            new UsuarioInicial(configuracion["OPERADOR_CORREO"], configuracion["OPERADOR_CONTRASENA"], Roles.Operador),
        };

        var crearUsuarios = alcance.ServiceProvider.GetRequiredService<CrearUsuariosIniciales>();
        var resultados = await crearUsuarios.EjecutarAsync(usuarios);

        foreach (var resultado in resultados)
        {
            Registrar(registro, resultado);
        }
    }

    private static void Registrar(ILogger registro, ResultadoDeUsuarioInicial resultado)
    {
        // Si ese nivel del registro está apagado, no se gasta tiempo armando los argumentos del mensaje.
        var esNormal = resultado.Estado is EstadoDeUsuarioInicial.Creado or EstadoDeUsuarioInicial.YaExistia;

        if (!registro.IsEnabled(esNormal ? LogLevel.Information : LogLevel.Warning))
        {
            return;
        }

        switch (resultado.Estado)
        {
            case EstadoDeUsuarioInicial.Creado:
                registro.LogInformation("Usuario inicial creado: {Correo} ({Rol}).", resultado.Correo, resultado.Rol);
                break;
            case EstadoDeUsuarioInicial.YaExistia:
                registro.LogInformation("El usuario inicial {Correo} ({Rol}) ya existía; no se cambió.", resultado.Correo, resultado.Rol);
                break;
            default:
                registro.LogWarning(
                    "No se creó el usuario inicial con rol {Rol}: {Motivo}. Revisa las variables en .env (copia .env.example).",
                    resultado.Rol,
                    resultado.Motivo);
                break;
        }
    }
}
