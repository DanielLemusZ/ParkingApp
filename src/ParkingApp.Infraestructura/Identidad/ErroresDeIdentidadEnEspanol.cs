using System.Globalization;

using Microsoft.AspNetCore.Identity;

namespace ParkingApp.Infraestructura.Identidad;

/// <summary>
/// Mensajes de Identity en español para los errores que puede ver un usuario: la política de contraseñas,
/// la contraseña actual incorrecta y los correos repetidos. Los demás quedan con el texto original.
/// </summary>
internal sealed class ErroresDeIdentidadEnEspanol : IdentityErrorDescriber
{
    public override IdentityError PasswordTooShort(int length)
    {
        return Error(nameof(PasswordTooShort), string.Format(CultureInfo.InvariantCulture, "La contraseña debe tener al menos {0} caracteres.", length));
    }

    public override IdentityError PasswordRequiresDigit()
    {
        return Error(nameof(PasswordRequiresDigit), "La contraseña debe tener al menos un número.");
    }

    public override IdentityError PasswordRequiresLower()
    {
        return Error(nameof(PasswordRequiresLower), "La contraseña debe tener al menos una minúscula.");
    }

    public override IdentityError PasswordRequiresUpper()
    {
        return Error(nameof(PasswordRequiresUpper), "La contraseña debe tener al menos una mayúscula.");
    }

    public override IdentityError PasswordRequiresNonAlphanumeric()
    {
        return Error(nameof(PasswordRequiresNonAlphanumeric), "La contraseña debe tener al menos un símbolo, por ejemplo - o *.");
    }

    public override IdentityError PasswordMismatch()
    {
        return Error(nameof(PasswordMismatch), "La contraseña actual no es correcta.");
    }

    public override IdentityError DuplicateEmail(string email)
    {
        return Error(nameof(DuplicateEmail), $"Ya existe un usuario con el correo {email}.");
    }

    public override IdentityError DuplicateUserName(string userName)
    {
        return Error(nameof(DuplicateUserName), $"Ya existe un usuario con el nombre {userName}.");
    }

    private static IdentityError Error(string codigo, string descripcion)
    {
        return new IdentityError { Code = codigo, Description = descripcion };
    }
}
