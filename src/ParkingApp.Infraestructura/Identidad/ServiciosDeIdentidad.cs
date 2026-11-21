using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

using ParkingApp.Aplicacion.Identidad;
using ParkingApp.Infraestructura.Persistencia;

namespace ParkingApp.Infraestructura.Identidad;

/// <summary>
/// Registro de ASP.NET Core Identity con cookies (F-10): usuarios y roles en PostgreSQL, bloqueo tras 5 intentos
/// fallidos y una cookie de sesión que en la API responde 401 o 403 en lugar de redirigir.
/// </summary>
public static class ServiciosDeIdentidad
{
    /// <summary>
    /// Dirección de la página de inicio de sesión.
    /// </summary>
    public const string RutaIniciarSesion = "/cuenta/iniciar-sesion";

    /// <summary>
    /// Dirección de la página que se muestra cuando el rol no alcanza.
    /// </summary>
    public const string RutaAccesoDenegado = "/cuenta/acceso-denegado";

    /// <summary>
    /// Intentos fallidos seguidos que bloquean la cuenta (criterio 7 del issue #23).
    /// </summary>
    public const int IntentosAntesDelBloqueo = 5;

    /// <summary>
    /// Tiempo que dura el bloqueo de la cuenta.
    /// </summary>
    public static readonly TimeSpan DuracionDelBloqueo = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Registra Identity, la autenticación por cookie y el directorio de usuarios.
    /// </summary>
    /// <param name="servicios">Contenedor de servicios de la aplicación.</param>
    /// <returns>El mismo contenedor de servicios.</returns>
    public static IServiceCollection AgregarIdentidad(this IServiceCollection servicios)
    {
        // 1. Autenticación: el esquema por defecto es la cookie de Identity. AddIdentityCookies registra también
        // las cookies auxiliares (por ejemplo, la de un segundo factor), aunque el MVP no las usa.
        servicios.AddAuthentication(IdentityConstants.ApplicationScheme)
            .AddIdentityCookies();

        // 2. Identity "core": usuarios, roles, la tienda en EF Core y el SignInManager, sin las páginas de la plantilla
        // (registro público, confirmación de correo...), que esta historia no quiere.
        servicios.AddIdentityCore<Usuario>(opciones =>
            {
                opciones.User.RequireUniqueEmail = true;
                opciones.SignIn.RequireConfirmedAccount = false;

                // Política de contraseñas: los valores por defecto de Identity (mayúscula, minúscula, número y
                // símbolo), con un largo mínimo de 8 en lugar de 6.
                opciones.Password.RequiredLength = 8;

                // Bloqueo por fuerza bruta: al 5.º intento fallido seguido, la cuenta queda bloqueada 5 minutos.
                opciones.Lockout.MaxFailedAccessAttempts = IntentosAntesDelBloqueo;
                opciones.Lockout.DefaultLockoutTimeSpan = DuracionDelBloqueo;
                opciones.Lockout.AllowedForNewUsers = true;
            })
            .AddRoles<IdentityRole<Guid>>()
            .AddEntityFrameworkStores<ParkingAppDbContext>()
            .AddSignInManager()
            .AddDefaultTokenProviders()
            .AddErrorDescriber<ErroresDeIdentidadEnEspanol>();

        // 3. La cookie de sesión.
        servicios.ConfigureApplicationCookie(cookie =>
        {
            cookie.Cookie.Name = "ParkingApp.Sesion";

            // HttpOnly: el JavaScript de la página no puede leer la cookie, así que un script inyectado no la roba.
            cookie.Cookie.HttpOnly = true;

            // Lax: el navegador no la manda en formularios que otro sitio envíe hacia el nuestro (ayuda contra CSRF).
            cookie.Cookie.SameSite = SameSiteMode.Lax;

            // La sesión dura un turno de 8 horas y se renueva mientras el usuario siga usando la aplicación.
            cookie.ExpireTimeSpan = TimeSpan.FromHours(8);
            cookie.SlidingExpiration = true;

            cookie.LoginPath = RutaIniciarSesion;
            cookie.AccessDeniedPath = RutaAccesoDenegado;

            // Una pantalla sin sesión redirige al inicio de sesión, pero un cliente de la API (Swagger, una cámara)
            // no sabe qué hacer con una redirección a una página HTML: se le responde 401 o 403 (criterios 3 y 5).
            cookie.Events.OnRedirectToLogin = contexto => ResponderORedirigir(contexto, StatusCodes.Status401Unauthorized);
            cookie.Events.OnRedirectToAccessDenied = contexto => ResponderORedirigir(contexto, StatusCodes.Status403Forbidden);
        });

        servicios.AddScoped<IDirectorioDeUsuarios, DirectorioDeUsuariosConIdentity>();

        return servicios;
    }

    private static Task ResponderORedirigir(RedirectContext<CookieAuthenticationOptions> contexto, int codigoParaLaApi)
    {
        if (contexto.Request.Path.StartsWithSegments("/api", StringComparison.OrdinalIgnoreCase))
        {
            contexto.Response.StatusCode = codigoParaLaApi;

            // La aplicación cambia las respuestas de error sin cuerpo por la página "no encontrado". Para la API se
            // apaga en esta respuesta: el cliente debe recibir el 401 o el 403 tal cual, no una página HTML.
            var paginasDeEstado = contexto.HttpContext.Features.Get<IStatusCodePagesFeature>();

            if (paginasDeEstado is not null)
            {
                paginasDeEstado.Enabled = false;
            }
        }
        else
        {
            contexto.Response.Redirect(contexto.RedirectUri);
        }

        return Task.CompletedTask;
    }
}
