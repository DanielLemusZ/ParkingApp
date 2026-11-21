using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using ParkingApp.Aplicacion.Identidad;

namespace ParkingApp.Infraestructura.Persistencia.Configuraciones;

/// <summary>
/// Semilla de los roles de Identity. Los roles sí van en la migración (y en el script de la entrega);
/// los usuarios no, porque llevarían una contraseña.
/// </summary>
internal sealed class RolConfiguracion : IEntityTypeConfiguration<IdentityRole<Guid>>
{
    // Todos los valores son fijos. Si el Id o el ConcurrencyStamp cambiaran en cada compilación, EF vería datos
    // distintos y generaría una migración nueva cada vez que alguien ejecuta "migrations add".
    private static readonly Guid IdOperador = new Guid("6f1d2c3a-0a4b-4c1e-9b7a-1f2e3d4c5b01");
    private static readonly Guid IdAdministrador = new Guid("6f1d2c3a-0a4b-4c1e-9b7a-1f2e3d4c5b02");

    public void Configure(EntityTypeBuilder<IdentityRole<Guid>> builder)
    {
        builder.HasData(
            Rol(IdOperador, Roles.Operador, "b3e1a6f2-5c47-4d0e-8a91-0c2f7d6e5a01"),
            Rol(IdAdministrador, Roles.Administrador, "b3e1a6f2-5c47-4d0e-8a91-0c2f7d6e5a02"));
    }

    // Identity busca los roles por NormalizedName (el nombre en mayúsculas), así que también se siembra.
    private static IdentityRole<Guid> Rol(Guid id, string nombre, string sello)
    {
        return new IdentityRole<Guid>
        {
            Id = id,
            Name = nombre,
            NormalizedName = nombre.ToUpperInvariant(),
            ConcurrencyStamp = sello,
        };
    }
}
