using Microsoft.AspNetCore.Mvc;

namespace ParkingApp.Web.Apariencia;

/// <summary>
/// Guarda en una cookie si el usuario prefiere el tema claro o el oscuro. El oscuro es el de los mockups (#71) y es el
/// que se usa si no hay preferencia.
/// </summary>
/// <remarks>
/// Es una cookie y no <c>localStorage</c> porque las páginas de cuenta son estáticas (D-11): se leen en el servidor
/// antes del primer render, así que todas las páginas salen con el tema correcto, sin parpadeo.
/// </remarks>
public static class PreferenciaDeTema
{
    /// <summary>
    /// Dirección del endpoint que cambia el tema.
    /// </summary>
    public const string RutaCambiarTema = "/preferencias/tema";

    /// <summary>
    /// Valor del formulario y de la cookie para el tema claro.
    /// </summary>
    public const string Claro = "claro";

    /// <summary>
    /// Valor del formulario y de la cookie para el tema oscuro.
    /// </summary>
    public const string Oscuro = "oscuro";

    private const string NombreDeLaCookie = "ParkingApp.Tema";

    /// <summary>
    /// Indica si la solicitud pide el tema oscuro.
    /// </summary>
    /// <param name="solicitud">Solicitud HTTP del usuario.</param>
    /// <returns><see langword="true"/> salvo que la cookie diga <c>claro</c>.</returns>
    public static bool EsOscuro(HttpRequest solicitud)
    {
        ArgumentNullException.ThrowIfNull(solicitud);

        return solicitud.Cookies[NombreDeLaCookie] != Claro;
    }

    /// <summary>
    /// Registra <c>POST /preferencias/tema</c>.
    /// </summary>
    /// <param name="rutas">Rutas de la aplicación.</param>
    /// <returns>Las mismas rutas.</returns>
    public static IEndpointRouteBuilder MapearPreferenciaDeTema(this IEndpointRouteBuilder rutas)
    {
        // POST con [FromForm]: ASP.NET exige el token antiforgery que envía el botón del encabezado.
        rutas.MapPost(RutaCambiarTema, (HttpContext http, [FromForm] string? tema, [FromForm] string? volverA) =>
        {
            var elegido = tema == Claro ? Claro : Oscuro;

            // No es un dato de sesión ni sensible: dura un año y no depende de quién inició sesión.
            http.Response.Cookies.Append(NombreDeLaCookie, elegido, new CookieOptions
            {
                HttpOnly = true,
                IsEssential = true,
                SameSite = SameSiteMode.Lax,
                Secure = http.Request.IsHttps,
                MaxAge = TimeSpan.FromDays(365),
            });

            return TypedResults.LocalRedirect(DireccionLocal(volverA));
        })
        .ExcludeFromDescription();

        return rutas;
    }

    // Solo se vuelve a direcciones de esta aplicación, igual que al iniciar sesión.
    private static string DireccionLocal(string? direccion)
    {
        var esLocal = direccion is not null
            && direccion.StartsWith('/')
            && !direccion.StartsWith("//", StringComparison.Ordinal)
            && !direccion.StartsWith("/\\", StringComparison.Ordinal);

        return esLocal ? direccion! : "/";
    }
}
