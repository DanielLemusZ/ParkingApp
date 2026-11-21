using Microsoft.AspNetCore.Identity;

namespace ParkingApp.Infraestructura.Identidad;

/// <summary>
/// Usuario de la aplicación para ASP.NET Core Identity. Vive en la infraestructura: para el dominio, un usuario es
/// solo un identificador. El identificador es un <see cref="Guid"/>, como el resto de las entidades.
/// </summary>
/// <remarks>
/// Identity ya trae lo necesario: correo, hash de la contraseña, contador de intentos fallidos y fin del bloqueo.
/// Por ahora no hace falta agregar campos propios.
/// </remarks>
public sealed class Usuario : IdentityUser<Guid>
{
}
