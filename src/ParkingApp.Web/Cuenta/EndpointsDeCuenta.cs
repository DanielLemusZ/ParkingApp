using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

using ParkingApp.Infraestructura.Identidad;

namespace ParkingApp.Web.Cuenta;

/// <summary>
/// Endpoints HTTP de la cuenta que no son una página: cerrar sesión.
/// </summary>
public static class EndpointsDeCuenta
{
    /// <summary>
    /// Dirección del endpoint que cierra la sesión.
    /// </summary>
    public const string RutaCerrarSesion = "/cuenta/cerrar-sesion";

    /// <summary>
    /// Registra <c>POST /cuenta/cerrar-sesion</c>.
    /// </summary>
    /// <param name="rutas">Rutas de la aplicación.</param>
    /// <returns>Las mismas rutas.</returns>
    public static IEndpointRouteBuilder MapearEndpointsDeCuenta(this IEndpointRouteBuilder rutas)
    {
        // Es POST y no GET a propósito: un enlace o una imagen en otra página no puede cerrar la sesión del usuario.
        // [FromForm] hace que ASP.NET exija el token antiforgery que el formulario del menú envía.
        rutas.MapPost(RutaCerrarSesion, async (SignInManager<Usuario> sesiones, [FromForm] string? motivo) =>
        {
            // Borra la cookie de sesión del navegador.
            await sesiones.SignOutAsync();
            return TypedResults.LocalRedirect(ServiciosDeIdentidad.RutaIniciarSesion);
        })
        .ExcludeFromDescription();

        return rutas;
    }
}
